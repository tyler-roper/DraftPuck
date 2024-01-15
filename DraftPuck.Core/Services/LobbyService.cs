using DraftPuck.Data.Data;

namespace DraftPuck.Core.Services;

public class LobbyService : ILobbyService
{
    private readonly DraftPuckContext _dbContext;
    private readonly ILobbyEventService _lobbyEventService;
    private static readonly Random _random = new();

    public LobbyService(DraftPuckContext dbContext, ILobbyEventService lobbyEventService)
    {
        _dbContext = dbContext;
        _lobbyEventService = lobbyEventService;
    }

    public async Task<Lobby> CreateLobby(Guid userId, NewLobbyRequest request)
    {
        Guid newLobbyId = Guid.NewGuid();
        Lobby lobby = new()
        {
            Id = newLobbyId,
            JoinCode = await RandomString(4),
            Status = 0,
            CreatedBy = userId,
            PicksPerTeam = request.PicksPerTeam
        };

        _ = _dbContext.Lobbies.Add(lobby);
        _ = _dbContext.LobbyMembers.Add(new LobbyMember()
        {
            LobbyId = newLobbyId,
            UserId = userId,
            Name = request.Name
        });

        _ = await _dbContext.SaveChangesAsync();

        return lobby;
    }

    public async Task<Lobby?> GetLobby(string joinCode, bool includeRemovedUsers = false)
    {
        return await _dbContext.Lobbies
                    .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved || includeRemovedUsers))
                        .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                            .ThenInclude(lmp => lmp.Drinks)
                    .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved || includeRemovedUsers))
                        .ThenInclude(lm => lm.Messages.Where(m => !m.IsDeleted))
                    .FirstOrDefaultAsync(l => l.JoinCode == joinCode);
    }

    public async Task<Lobby?> GetLobby(Guid lobbyId)
    {
        return await _dbContext.Lobbies
                    .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
                        .ThenInclude(lm => lm.LobbyMemberPicks.Where(lmp => lmp.IsActive))
                            .ThenInclude(lmp => lmp.Drinks)
                    .Include(l => l.LobbyMembers.Where(lm => !lm.IsRemoved))
                        .ThenInclude(lm => lm.Messages.Where(m => !m.IsDeleted))
                    .FirstOrDefaultAsync(l => l.Id == lobbyId);
    }

    public async Task<List<LobbyEvent>> GetLobbyEvents(Guid userId, Guid lobbyId)
    {
        if (!await UserIsInLobby(userId, lobbyId))
        {
            throw new Exception("User not in lobby.");
        }

        Lobby? lobby = await _dbContext.Lobbies.FindAsync(lobbyId);
        return lobby == null
            ? (List<LobbyEvent>)(new())
            : await _dbContext.LobbyEvents
            .Where(lobbyEvent => lobbyEvent.LobbyId == lobbyId || (lobbyEvent.LobbyId == null && lobbyEvent.Created >= lobby.Created && lobbyEvent.Created <= lobby.Created.AddHours(12)))
            .OrderBy(lobbyEvent => lobbyEvent.TimeUtc)
            .ToListAsync();
    }

    public async Task<bool> UserIsInLobby(Guid userId, Guid lobbyId)
    {
        return await _dbContext.LobbyMembers.Where(lm => !lm.IsRemoved).AnyAsync(lm => lm.UserId == userId && lm.LobbyId == lobbyId);
    }

    public async Task<bool> UserIsInLobby(Guid userId, string joinCode)
    {
        return await _dbContext.LobbyMembers.Include(lm => lm.Lobby).Where(lm => !lm.IsRemoved).AnyAsync(lm => lm.UserId == userId && lm.Lobby.JoinCode == joinCode);
    }

    public async Task<Lobby?> JoinLobbyByCode(Guid userId, string joinCode, JoinLobbyRequest request)
    {
        if (request.IsBot)
        {
            _ = _dbContext.Users.Add(new User() { Id = userId, IsBot = true });
            _ = await _dbContext.SaveChangesAsync();
        }

        Lobby? lobby = await GetLobby(joinCode, true);
        if (lobby == null)
        {
            return null;
        }

        LobbyMember? existingLobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
        if (existingLobbyMember == null)
        {
            //JOINED LOBBY
            LobbyMember lobbyMember = new()
            {
                LobbyId = lobby.Id,
                UserId = userId,
                Name = request.Name,
                IsBot = request.IsBot,
                BotPickStyle = request.BotPickStyle
            };

            _ = _dbContext.LobbyMembers.Add(lobbyMember);
            _ = await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendUserJoinedEvent(lobby, lobbyMember);
        }
        else if (!existingLobbyMember.IsRemoved && existingLobbyMember.Name != request.Name)
        {
            //NAME CHANGE
            string previousName = existingLobbyMember.Name;
            existingLobbyMember.Name = request.Name;
            _ = await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendUserNameChangedEvent(lobby, existingLobbyMember, previousName);
        }
        else if (existingLobbyMember.IsRemoved)
        {
            //REJOINED AFTER REMOVAL
            string previousName = existingLobbyMember.Name;
            existingLobbyMember.Name = request.Name;
            existingLobbyMember.IsRemoved = false;

            foreach (LobbyMemberPick pick in existingLobbyMember.LobbyMemberPicks)
            {
                bool wasRePicked = lobby.LobbyMembers
                    .SelectMany(lm => lm.LobbyMemberPicks)
                    .Any(lmp => lmp.IsActive && lmp.PlayerId == pick.PlayerId);

                if (!wasRePicked)
                {
                    pick.IsActive = true;
                }
            }

            _ = await _dbContext.SaveChangesAsync();
            await _lobbyEventService.SendUserRejoinedEvent(lobby, existingLobbyMember);

            if (previousName != existingLobbyMember.Name)
            {
                await _lobbyEventService.SendUserNameChangedEvent(lobby, existingLobbyMember, previousName);
            }
        }

        return lobby;
    }

    public async Task<LobbyMemberPick> MakePick(Guid userId, string joinCode, MakePickRequest request)
    {
        Lobby? lobby = await GetLobby(joinCode);
        if (lobby == null)
        {
            throw new KeyNotFoundException("Lobby not found.");
        }

        LobbyMember? lobbyMember = request.LobbyMemberId == null
            ? lobby.LobbyMembers.SingleOrDefault(lm => !lm.IsRemoved && lm.UserId == userId)
            : lobby.LobbyMembers.FirstOrDefault(lm => !lm.IsRemoved && lm.Id == request.LobbyMemberId);

        if (lobbyMember == null)
        {
            throw new KeyNotFoundException("UserId not found in lobby.");
        }

        if (lobby.LobbyMembers.Where(lm => !lm.IsRemoved).SelectMany(lm => lm.LobbyMemberPicks).Any(pick => pick.IsActive && pick.PlayerId == request.PlayerId && pick.GameId == request.GameId))
        {
            throw new InvalidOperationException("Player already picked.");
        }

        LobbyMemberPick pick = new()
        {
            LobbyMemberId = lobbyMember.Id,
            PlayerId = request.PlayerId,
            GameId = request.GameId,
            TeamId = request.TeamId
        };

        _ = _dbContext.LobbyMemberPicks.Add(pick);
        _ = await _dbContext.SaveChangesAsync();

        await _lobbyEventService.SendNewPickEvent(lobby, lobbyMember, request.GameId, request.PlayerId, request.TeamId);

        return pick;
    }

    public async Task RemovePick(Guid currentUserId, string joinCode, Guid lobbyMemberPickId)
    {
        Lobby? lobby = await GetLobby(joinCode);
        if (lobby == null)
        {
            throw new KeyNotFoundException("Lobby not found.");
        }

        LobbyMemberPick? pick = lobby.LobbyMembers.SelectMany(lm => lm.LobbyMemberPicks).FirstOrDefault(lmp => lmp.Id == lobbyMemberPickId);
        if (pick == null)
        {
            throw new KeyNotFoundException("Pick not found.");
        }

        if (pick.LobbyMember.UserId != currentUserId && lobby.CreatedBy != currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        pick.IsActive = false;
        _ = await _dbContext.SaveChangesAsync();
        await _lobbyEventService.SendPickRemovedEvent(lobby, pick.LobbyMember, pick.GameId, pick.PlayerId, pick.TeamId);
    }

    public async Task<Drink> AssignDrink(Guid userId, string joinCode, Guid drinkId, Guid recipientLobbyMemberId)
    {
        Lobby? lobby = await GetLobby(joinCode);
        if (lobby == null)
        {
            throw new KeyNotFoundException("Lobby not found.");
        }

        LobbyMember? sender = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
        if (sender == null)
        {
            throw new KeyNotFoundException("Sender UserId not found in lobby.");
        }

        LobbyMember? recipient = lobby.LobbyMembers.FirstOrDefault(member => member.Id == recipientLobbyMemberId);
        if (recipient == null)
        {
            throw new KeyNotFoundException("Recipient UserId not found in lobby.");
        }

        Drink? drink = lobby.LobbyMembers.SelectMany(member => member.LobbyMemberPicks).SelectMany(lmp => lmp.Drinks).FirstOrDefault(d => d.Id == drinkId);
        if (drink == null)
        {
            throw new KeyNotFoundException("DrinkId not found in lobby.");
        }

        if (drink.RecipientLobbyMemberId != null)
        {
            throw new InvalidOperationException("Drink already has recipient assigned.");
        }

        drink.Assigned = DateTime.UtcNow;
        drink.RecipientLobbyMemberId = recipient.Id;
        _ = await _dbContext.SaveChangesAsync();

        await _lobbyEventService.SendDrinkAssignedEvent(lobby, sender, recipient, drink.LobbyMemberPick.GameId, drink.EventId, drink.LobbyMemberPick.PlayerId, drink.LobbyMemberPick.TeamId);

        return drink;
    }

    public async Task ChangeName(Guid userId, string joinCode, string newName)
    {
        Lobby? lobby = await GetLobby(joinCode);
        if (lobby == null)
        {
            throw new KeyNotFoundException("Lobby not found.");
        }

        LobbyMember? lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
        if (lobbyMember == null)
        {
            throw new KeyNotFoundException("UserId not found in lobby.");
        }

        string oldName = lobbyMember.Name;

        lobbyMember.Name = newName;
        _ = await _dbContext.SaveChangesAsync();

        await _lobbyEventService.SendUserNameChangedEvent(lobby, lobbyMember, oldName);
    }

    public async Task RemoveLobbyMember(Guid currentUserId, string joinCode, Guid lobbyMemberId)
    {
        Lobby? lobby = await GetLobby(joinCode);
        if (lobby == null)
        {
            throw new KeyNotFoundException("Lobby not found.");
        }

        if (lobby.CreatedBy != currentUserId)
        {
            throw new UnauthorizedAccessException();
        }

        LobbyMember? lobbyMemberToRemove = lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == lobbyMemberId);
        if (lobbyMemberToRemove == null)
        {
            return;
        }

        lobbyMemberToRemove.IsRemoved = true;
        foreach (LobbyMemberPick pick in lobbyMemberToRemove.LobbyMemberPicks)
        {
            pick.IsActive = false;
        }

        _ = await _dbContext.SaveChangesAsync();

        await _lobbyEventService.SendUserRemovedEvent(lobby, lobbyMemberToRemove);
    }

    public async Task DeleteOldLobbies()
    {
        List<Lobby> oldLobbies = await _dbContext.Lobbies
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
                    .ThenInclude(lmp => lmp.Drinks)
            .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.Messages)
            .Where(l => l.Created <= DateTime.UtcNow.AddDays(-2))
            .ToListAsync();

        IEnumerable<LobbyMember> members = oldLobbies.SelectMany(l => l.LobbyMembers);
        IEnumerable<LobbyMemberPick> picks = members.SelectMany(lm => lm.LobbyMemberPicks);
        IEnumerable<Drink> drinks = picks.SelectMany(lmp => lmp.Drinks);
        IEnumerable<MessageEntity> messages = members.SelectMany(lm => lm.Messages);
        _dbContext.Messages.RemoveRange(messages);
        _dbContext.Drinks.RemoveRange(drinks);
        _dbContext.LobbyMemberPicks.RemoveRange(picks);
        _dbContext.LobbyMembers.RemoveRange(members);
        _dbContext.Lobbies.RemoveRange(oldLobbies);

        List<LobbyEvent> oldLobbyEvents = await _dbContext.LobbyEvents.Where(le => le.Created <= DateTime.UtcNow.AddDays(-2)).ToListAsync();
        _dbContext.LobbyEvents.RemoveRange(oldLobbyEvents);

        _ = await _dbContext.SaveChangesAsync();
    }

    public async Task Broadcast(string joinCode, string message)
    {
        Lobby? lobby = await GetLobby(joinCode);
        if (lobby == null)
        {
            throw new KeyNotFoundException("Lobby not found.");
        }

        await _lobbyEventService.Broadcast(lobby, message);
    }

    public async Task SendMessage(Guid userId, string joinCode, string message)
    {
        Lobby? lobby = await GetLobby(joinCode);
        if (lobby == null)
        {
            throw new KeyNotFoundException("Lobby not found.");
        }

        LobbyMember? lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == userId);
        if (lobbyMember == null)
        {
            throw new KeyNotFoundException("UserId not found in lobby.");
        }

        MessageEntity messageEntity = new()
        {
            LobbyMemberId = lobbyMember.Id,
            Message = message
        };

        _ = _dbContext.Messages.Add(messageEntity);
        _ = await _dbContext.SaveChangesAsync();

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
        bool stillSearching = true;
        string result = "";

        while (stillSearching)
        {
            result = new string(Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray());
            stillSearching = await _dbContext.Lobbies.AnyAsync(l => l.JoinCode == result);
        }

        return result;
    }
}
