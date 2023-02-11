namespace DraftPuck.Services
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

        public async Task<Lobby?> GetLobby(string joinCode)
            => await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                    .ThenInclude(lm => lm.LobbyMemberPicks)
                        .ThenInclude(lmp => lmp.Drinks)
                .FirstOrDefaultAsync(l => l.JoinCode == joinCode);

        public async Task<Lobby?> GetLobby(Guid lobbyId)
            => await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                    .ThenInclude(lm => lm.LobbyMemberPicks)
                        .ThenInclude(lmp => lmp.Drinks)
                .FirstOrDefaultAsync(l => l.Id == lobbyId);

        public async Task<List<LobbyEvent>> GetLobbyEvents(Guid userId, Guid lobbyId)
        {
            if (!await UserIsInLobby(userId, lobbyId)) throw new Exception("User not in lobby.");

            var lobby = await _dbContext.Lobbies.FindAsync(lobbyId);
            if (lobby == null) return new();

            return await _dbContext.LobbyEvents
                .Where(lobbyEvent => lobbyEvent.LobbyId == lobbyId || (lobbyEvent.LobbyId == null && lobbyEvent.Created >= lobby.Created && lobbyEvent.Created <= lobby.Created.AddHours(12)))
                .OrderBy(lobbyEvent => lobbyEvent.TimeUtc)
                .ToListAsync();
        }

        public async Task<bool> UserIsInLobby(Guid userId, Guid lobbyId)
            => await _dbContext.LobbyMembers.AnyAsync(lm => lm.UserId == userId && lm.LobbyId == lobbyId);

        public async Task<bool> UserIsInLobby(Guid userId, string joinCode)
            => await _dbContext.LobbyMembers.Include(lm => lm.Lobby).AnyAsync(lm => lm.UserId == userId && lm.Lobby.JoinCode == joinCode);

        public async Task<Lobby?> JoinLobbyByCode(Guid userId, string joinCode, JoinLobbyRequest request)
        {
            if (request.IsBot)
            {
                _dbContext.Users.Add(new User() { Id = userId, IsBot = true });
                await _dbContext.SaveChangesAsync();
            }

            var lobby = await GetLobby(joinCode);
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
            else if (existingLobbyMember.Name != request.Name)
            {
                //NAME CHANGE
                var lobbyMemberEntity = await _dbContext.LobbyMembers.FindAsync(existingLobbyMember.Id);
                var previousName = lobbyMemberEntity.Name;
                lobbyMemberEntity.Name = request.Name;
                await _dbContext.SaveChangesAsync();

                await _lobbyEventService.SendUserNameChangedEvent(lobby, lobbyMemberEntity, previousName);
            }

            return lobby;
        }

        public async Task<LobbyMemberPick> MakePick(Guid userId, string joinCode, MakePickRequest request)
        {
            var lobby = await GetLobby(joinCode);
            if (lobby == null) throw new KeyNotFoundException("Lobby not found.");

            var lobbyMember = request.LobbyMemberId == null
                ? lobby.LobbyMembers.SingleOrDefault(lm => lm.UserId == userId)
                : lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == request.LobbyMemberId);

            if (lobbyMember == null) throw new KeyNotFoundException("UserId not found in lobby.");

            if (lobby.LobbyMembers.SelectMany(lm => lm.LobbyMemberPicks).Any(pick => pick.PlayerId == request.PlayerId && pick.GamePk == request.GamePk)) 
                throw new InvalidOperationException("Player already picked.");

            var pick = new LobbyMemberPick()
            {
                LobbyMemberId = lobbyMember.Id,
                PlayerId = request.PlayerId,
                GamePk = request.GamePk,
                TeamId = request.TeamId
            };
             
            _dbContext.LobbyMemberPicks.Add(pick);
            await _dbContext.SaveChangesAsync();

            await _lobbyEventService.SendNewPickEvent(lobby, lobbyMember, request.GamePk, request.PlayerId, request.TeamId);

            return pick;
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

            await _lobbyEventService.SendDrinkAssignedEvent(lobby, sender, recipient, drink.LobbyMemberPick.GamePk, drink.EventId, drink.LobbyMemberPick.PlayerId, drink.LobbyMemberPick.TeamId);

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
