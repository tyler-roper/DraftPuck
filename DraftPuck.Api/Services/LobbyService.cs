using Azure.Core;
using DraftPuck.Data.Entities;
using System.Reflection;

namespace DraftPuck.Api.Services
{
    public class LobbyService : ILobbyService
    {
        private readonly DraftPuckContext _dbContext;
        private readonly ILobbyEventService _lobbyEventService;
        private static Random _random = new Random();

        public LobbyService(DraftPuckContext dbContext, ILobbyEventService lobbyEventService)
        {
            _dbContext = dbContext;
            _lobbyEventService = lobbyEventService;
        }

        public async Task<Lobby> CreateLobby(Guid userId, NewLobbyRequest request)
        {
            var newLobbyId = Guid.NewGuid();
            var lobby = new Lobby()
            {
                Id = newLobbyId,
                JoinCode = await RandomString(4),
                Status = 0,
                CreatedBy = userId,
                PicksPerTeam = request.PicksPerTeam
            };

            _dbContext.Lobbies.Add(lobby);
            _dbContext.LobbyMembers.Add(new LobbyMember()
            {
                LobbyId = newLobbyId,
                UserId = userId,
                Name = request.Name
            });

            await _dbContext.SaveChangesAsync();

            return lobby;
        }

        public async Task<Lobby?> GetLobby(string joinCode, bool includeRemovedUsers = false)
            => await _dbContext.Lobbies
                .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved || includeRemovedUsers))
                    .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                        .ThenInclude(lmp => lmp.Drinks)
                .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved || includeRemovedUsers))
                    .ThenInclude(lm => lm.Messages.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(l => l.JoinCode == joinCode);

        public async Task<Lobby?> GetLobby(Guid lobbyId)
            => await _dbContext.Lobbies
                .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
                    .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                        .ThenInclude(lmp => lmp.Drinks)
                .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
                    .ThenInclude(lm => lm.Messages.Where(m => !m.IsDeleted))
                .FirstOrDefaultAsync(l => l.Id == lobbyId);

        public async Task<List<LobbyEvent>> GetLobbyEvents(Guid userId, Guid lobbyId)
        {
            if (!await UserIsInLobby(userId, lobbyId)) throw new Exception("User not in lobby.");

            var lobby = await _dbContext.Lobbies.FindAsync(lobbyId);
            if (lobby == null) return new();

            return await _dbContext.LobbyEvents
                .Where(lobbyEvent => lobbyEvent.LobbyId == lobbyId || lobbyEvent.LobbyId == null && lobbyEvent.Created >= lobby.Created && lobbyEvent.Created <= lobby.Created.AddHours(12))
                .OrderBy(lobbyEvent => lobbyEvent.TimeUtc)
                .ToListAsync();
        }

        public async Task<bool> UserIsInLobby(Guid userId, Guid lobbyId)
            => await _dbContext.LobbyMembers.Where(lm => !lm.IsRemoved).AnyAsync(lm => lm.UserId == userId && lm.LobbyId == lobbyId);

        public async Task<bool> UserIsInLobby(Guid userId, string joinCode)
            => await _dbContext.LobbyMembers.Include(lm => lm.Lobby).Where(lm => !lm.IsRemoved).AnyAsync(lm => lm.UserId == userId && lm.Lobby.JoinCode == joinCode);

        public async Task<Lobby?> JoinLobbyByCode(Guid userId, string joinCode, JoinLobbyRequest request)
        {
            if (request.IsBot)
            {
                _dbContext.Users.Add(new User() { Id = userId, IsBot = true });
                await _dbContext.SaveChangesAsync();
            }

            var lobby = await GetLobby(joinCode, true);
            if (lobby == null) return null;

            var existingLobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
            if (existingLobbyMember == null)
            {
                //JOINED LOBBY
                var lobbyMember = new LobbyMember()
                {
                    LobbyId = lobby.Id,
                    UserId = userId,
                    Name = request.Name,
                    IsBot = request.IsBot,
                    BotPickStyle = request.BotPickStyle
                };

                _dbContext.LobbyMembers.Add(lobbyMember);
                await _dbContext.SaveChangesAsync();

                await _lobbyEventService.SendUserJoinedEvent(lobby, lobbyMember);
            }
            else if (!existingLobbyMember.IsRemoved && existingLobbyMember.Name != request.Name)
            {
                //NAME CHANGE
                var previousName = existingLobbyMember.Name;
                existingLobbyMember.Name = request.Name;
                await _dbContext.SaveChangesAsync();

                await _lobbyEventService.SendUserNameChangedEvent(lobby, existingLobbyMember, previousName);
            }
            else if (existingLobbyMember.IsRemoved)
            {
                //REJOINED AFTER REMOVAL
                var previousName = existingLobbyMember.Name;
                existingLobbyMember.Name = request.Name;
                existingLobbyMember.IsRemoved = false;

                foreach (var pick in existingLobbyMember.LobbyMemberPicks)
                {
                    var wasRePicked = lobby.LobbyMembers
                        .SelectMany(lm => lm.LobbyMemberPicks)
                        .Any(lmp => lmp.IsActive && lmp.PlayerId == pick.PlayerId);

                    if (!wasRePicked) pick.IsActive = true;
                }

                await _dbContext.SaveChangesAsync();
                await _lobbyEventService.SendUserRejoinedEvent(lobby, existingLobbyMember);

                if (previousName != existingLobbyMember.Name)
                    await _lobbyEventService.SendUserNameChangedEvent(lobby, existingLobbyMember, previousName);
            }

            return lobby;
        }

        public async Task<LobbyMemberPick> MakePick(Guid userId, string joinCode, MakePickRequest request)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");

            var lobbyMember = request.LobbyMemberId == null
                ? lobby.LobbyMembers.SingleOrDefault(lm => !lm.IsRemoved && lm.UserId == userId)
                : lobby.LobbyMembers.FirstOrDefault(lm => !lm.IsRemoved && lm.Id == request.LobbyMemberId);

            if (lobbyMember == null) throw new KeyNotFoundException("UserId not found in lobby.");

            if (lobby.LobbyMembers.Where(lm => !lm.IsRemoved).SelectMany(lm => lm.LobbyMemberPicks).Any(pick => pick.IsActive && pick.PlayerId == request.PlayerId && pick.GameId == request.GameId))
                throw new InvalidOperationException("Player already picked.");

            var pick = new LobbyMemberPick()
            {
                LobbyMemberId = lobbyMember.Id,
                PlayerId = request.PlayerId,
                GameId = request.GameId,
                TeamId = request.TeamId
            };

            _dbContext.LobbyMemberPicks.Add(pick);
            await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendNewPickEvent(lobby, lobbyMember, request.GameId, request.PlayerId, request.TeamId);

            return pick;
        }

        public async Task RemovePick(Guid currentUserId, string joinCode, Guid lobbyMemberPickId)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");

            var pick = lobby.LobbyMembers.SelectMany(lm => lm.LobbyMemberPicks).FirstOrDefault(lmp => lmp.Id == lobbyMemberPickId);
            if (pick == null) throw new KeyNotFoundException("Pick not found.");

            if (pick.LobbyMember.UserId != currentUserId && lobby.CreatedBy != currentUserId)
                throw new UnauthorizedAccessException();

            pick.IsActive = false;
            await _dbContext.SaveChangesAsync();
            await _lobbyEventService.SendPickRemovedEvent(lobby, pick.LobbyMember, pick.GameId, pick.PlayerId, pick.TeamId);
        }

        public async Task<Drink> AssignDrink(Guid userId, string joinCode, Guid drinkId, Guid recipientLobbyMemberId)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");
            var sender = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
            if (sender == null) throw new KeyNotFoundException("Sender UserId not found in lobby.");
            var recipient = lobby.LobbyMembers.FirstOrDefault(member => member.Id == recipientLobbyMemberId);
            if (recipient == null) throw new KeyNotFoundException("Recipient UserId not found in lobby.");

            var drink = lobby.LobbyMembers.SelectMany(member => member.LobbyMemberPicks).SelectMany(lmp => lmp.Drinks).FirstOrDefault(d => d.Id == drinkId);
            if (drink == null) throw new KeyNotFoundException("DrinkId not found in lobby.");
            if (drink.RecipientLobbyMemberId != null) throw new InvalidOperationException("Drink already has recipient assigned.");

            drink.Assigned = DateTime.UtcNow;
            drink.RecipientLobbyMemberId = recipient.Id;
            await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendDrinkAssignedEvent(lobby, sender, recipient, drink.LobbyMemberPick.GameId, drink.EventId, drink.LobbyMemberPick.PlayerId, drink.LobbyMemberPick.TeamId);

            return drink;
        }

        public async Task ChangeName(Guid userId, string joinCode, string newName)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");

            var lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
            if (lobbyMember == null) throw new KeyNotFoundException("UserId not found in lobby.");

            var oldName = lobbyMember.Name;

            lobbyMember.Name = newName;
            await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendUserNameChangedEvent(lobby, lobbyMember, oldName);
        }

        public async Task RemoveLobbyMember(Guid currentUserId, string joinCode, Guid lobbyMemberId)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");

            if (lobby.CreatedBy != currentUserId) throw new UnauthorizedAccessException();

            var lobbyMemberToRemove = lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == lobbyMemberId);
            if (lobbyMemberToRemove == null) return;

            lobbyMemberToRemove.IsRemoved = true;
            foreach (var pick in lobbyMemberToRemove.LobbyMemberPicks)
                pick.IsActive = false;

            await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendUserRemovedEvent(lobby, lobbyMemberToRemove);
        }

        public async Task DeleteOldLobbies()
        {
            var oldLobbies = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                    .ThenInclude(lm => lm.LobbyMemberPicks)
                        .ThenInclude(lmp => lmp.Drinks)
                .Include(l => l.LobbyMembers)
                    .ThenInclude(lm => lm.Messages)
                .Where(l => l.Created <= DateTime.UtcNow.AddDays(-2))
                .ToListAsync();

            var members = oldLobbies.SelectMany(l => l.LobbyMembers);
            var picks = members.SelectMany(lm => lm.LobbyMemberPicks);
            var drinks = picks.SelectMany(lmp => lmp.Drinks);
            var messages = members.SelectMany(lm => lm.Messages);
            _dbContext.Messages.RemoveRange(messages);
            _dbContext.Drinks.RemoveRange(drinks);
            _dbContext.LobbyMemberPicks.RemoveRange(picks);
            _dbContext.LobbyMembers.RemoveRange(members);
            _dbContext.Lobbies.RemoveRange(oldLobbies);

            var oldLobbyEvents = await _dbContext.LobbyEvents.Where(le => le.Created <= DateTime.UtcNow.AddDays(-2)).ToListAsync();
            _dbContext.LobbyEvents.RemoveRange(oldLobbyEvents);

            await _dbContext.SaveChangesAsync();
        }

        public async Task Broadcast(string joinCode, string message)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");

            await _lobbyEventService.Broadcast(lobby, message);
        }

        public async Task SendMessage(Guid userId, string joinCode, string message)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");

            var lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
            if (lobbyMember == null) throw new KeyNotFoundException("UserId not found in lobby.");

            var messageEntity = new MessageEntity()
            {
                LobbyMemberId = lobbyMember.Id,
                Message = message
            };

            _dbContext.Messages.Add(messageEntity);
            await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendMessage(joinCode, new MessageModel()
            {
                Id = messageEntity.Id,
                Sent = messageEntity.Sent,
                Message = messageEntity.Message,
                LobbyMemberId = messageEntity.LobbyMemberId
            });
        }

        private async Task<string> RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stillSearching = true;
            var result = "";

            while (stillSearching)
            {
                result = new string(Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray());
                stillSearching = await _dbContext.Lobbies.AnyAsync(l => l.JoinCode == result);
            }

            return result;
        }
    }
}
