using BrewPuck.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BrewPuck.Api
{
    public class LobbyController : BrewPuckApiControllerBase
    {
        private static Random random = new Random();
        private readonly BrewPuckContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IHubContext<LobbyHub> _hubContext;

        public LobbyController(BrewPuckContext dbContext, IMapper mapper, IHubContext<LobbyHub> hubContext)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLobby(NewLobbyRequest request)
        {
            if (CurrentUser == null) return Unauthorized();
            if (string.IsNullOrEmpty(request.Name)) return BadRequest();

            var newLobbyId = Guid.NewGuid();
            var lobby = new Lobby()
            {
                Id = newLobbyId,
                JoinCode = await RandomString(4),
                Status = 0,
                CreatedBy = CurrentUser.Id,
                PicksPerTeam = request.PicksPerTeam
            };

            _dbContext.Lobbies.Add(lobby);
            _dbContext.LobbyMembers.Add(new LobbyMember()
            {
                LobbyId = newLobbyId,
                UserId = CurrentUser.Id,
                Name = request.Name
            });

            await _dbContext.SaveChangesAsync();
            return Created($"lobbies/{newLobbyId}", _mapper.Map<LobbyResponse>(lobby));
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetLobbyByCode(string code)
        {
            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                    .ThenInclude(lm => lm.LobbyMemberPicks)
                        .ThenInclude(lmp => lmp.Drinks)
                .FirstOrDefaultAsync(l => l.JoinCode == code);

            return lobby == null
                ? NotFound()
                : Ok(_mapper.Map<LobbyResponse>(lobby));
        }

        [HttpPost("{code}/join")]
        public async Task<IActionResult> JoinLobbyByCode(string code, JoinLobbyRequest request)
        {
            if (CurrentUser == null && !request.IsBot) return Unauthorized();

            var userId = !request.IsBot
                ? CurrentUser!.Id
                : Guid.NewGuid();

            if (request.IsBot)
            {
                _dbContext.Users.Add(new User() { Id = userId });
                await _dbContext.SaveChangesAsync();
            }

            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                .FirstOrDefaultAsync(l => l.JoinCode == code);
            if (lobby == null) return NotFound();

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

                var lobbyEvent = new LobbyEventModel(LobbyEventType.UserJoined, lobby.Id, lobbyMember);
                await _hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent);

            } else if (existingLobbyMember.Name != request.Name)
            {
                //NAME CHANGE
                var lobbyMemberEntity = await _dbContext.LobbyMembers.FindAsync(existingLobbyMember.Id);
                lobbyMemberEntity.Name = request.Name;
                await _dbContext.SaveChangesAsync();

                var lobbyEvent = new LobbyEventModel(LobbyEventType.UserNameChanged, lobby.Id, lobbyMemberEntity);
                await _hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent);
            }

            return Ok(_mapper.Map<LobbyResponse>(lobby));
        }

        [HttpPost("{code}/pick")]
        public async Task<IActionResult> MakePick(string code, MakePickRequest request)
        {
            if (CurrentUser == null) return Unauthorized();
            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
                .FirstOrDefaultAsync(l => l.JoinCode == code);

            if (lobby == null) return NotFound();

            var lobbyMember = request.LobbyMemberId != null
                ? lobby.LobbyMembers.FirstOrDefault(lm => lm.Id == request.LobbyMemberId)
                : lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == CurrentUser.Id);

            if (lobbyMember == null) return Unauthorized();

            if (lobby.LobbyMembers.SelectMany(lm => lm.LobbyMemberPicks).Any(pick => pick.PlayerId == request.PlayerId && pick.GamePk == request.GamePk))
                return Conflict();

            var pick = new LobbyMemberPick()
            {
                LobbyMemberId = lobbyMember.Id,
                PlayerId = request.PlayerId,
                GamePk = request.GamePk
            };

            _dbContext.LobbyMemberPicks.Add(pick);
            await _dbContext.SaveChangesAsync();

            var lobbyEvent = new LobbyEventModel(LobbyEventType.NewPick, lobby.Id, pick);
            await _hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent);

            return Ok(_mapper.Map<LobbyMemberPickResponse>(pick));
        }

        [HttpPost("{code}/drink")]
        public async Task<IActionResult> Drink(string code, NewDrinkRequest request)
        {
            if (CurrentUser == null) return Unauthorized();
            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
                .ThenInclude(lmp => lmp.Drinks)
                .FirstOrDefaultAsync(l => l.JoinCode == code);

            if (lobby == null) return NotFound();

            var lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == CurrentUser.Id);
            if (lobbyMember == null) return Unauthorized();

            var lobbyMemberPick = lobby.LobbyMembers.SelectMany(lm => lm.LobbyMemberPicks).FirstOrDefault(lmp => lmp.Id == request.LobbyMemberPickId);
            if (lobbyMemberPick == null || (lobbyMemberPick.LobbyMember.UserId != CurrentUser.Id && !lobbyMemberPick.LobbyMember.IsBot)) return Unauthorized();

            var drink = new Drink()
            {
                LobbyMemberPickId = lobbyMemberPick.Id,
                RecipientLobbyMemberId = null,
                EventId = request.EventId
            };

            _dbContext.Drinks.Add(drink);
            await _dbContext.SaveChangesAsync();

            var lobbyEvent = new LobbyEventModel(LobbyEventType.NewDrink, lobby.Id, drink);
            await _hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent);

            return Ok(_mapper.Map<DrinkResponse>(drink));
        }

        [HttpPost("{code}/drink/{drinkId}/assign")]
        public async Task<IActionResult> AssignDrink(string code, Guid drinkId, Guid recipientLobbyMemberId)
        {
            if (CurrentUser == null) return Unauthorized();

            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                .ThenInclude(lm => lm.LobbyMemberPicks)
                .ThenInclude(lmp => lmp.Drinks)
                .FirstOrDefaultAsync(l => l.JoinCode == code);

            if (lobby == null) return NotFound();

            var lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == CurrentUser.Id);
            if (lobbyMember == null) return Unauthorized();

            var recipient = lobby.LobbyMembers.FirstOrDefault(member => member.Id == recipientLobbyMemberId);
            if (recipient == null) return NotFound();

            var drink = lobby.LobbyMembers.SelectMany(member => member.LobbyMemberPicks).SelectMany(lmp => lmp.Drinks).FirstOrDefault(d => d.Id == drinkId);
            if (drink == null) return NotFound();
            if (drink.RecipientLobbyMemberId != null) return Conflict();

            drink.RecipientLobbyMemberId = recipient.Id;
            await _dbContext.SaveChangesAsync();

            var lobbyEvent = new LobbyEventModel(LobbyEventType.DrinkAssigned, lobby.Id, drink);
            await _hubContext.Clients.All.SendAsync("LobbyEvent", lobbyEvent);

            return Ok(_mapper.Map<DrinkResponse>(drink));
        }

        private async Task<string> RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var stillSearching = true;
            var result = "";

            while (stillSearching) {
                result = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
                stillSearching = await _dbContext.Lobbies.AnyAsync(l => l.JoinCode == result);
            }

            return result;
        }
    }

    public class NewLobbyRequest
    {
        public string Name { get; set; } = null!;
        public int PicksPerTeam { get; set; }
    }

    public class LobbyResponse
    {
        public Guid Id { get; set; }
        public string JoinCode { get; set; } = null!;
        public LobbyStatus Status { get; set; }
        public int PicksPerTeam { get; set; }
        public DateTime Created { get; set; }
        public Guid CreatedBy { get; set; }
        public List<LobbyMemberResponse> Members { get; set; } = new();
    }

    public class LobbyMemberResponse
    {
        public Guid Id { get; set; }
        public Guid LobbyId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Joined { get; set; }
        public string Name { get; set; } = null!;
        public bool IsBot { get; set; }
        public BotPickStyle BotPickStyle { get; set; }
        public List<LobbyMemberPickResponse> Picks { get; set; } = new();
    }

    public class LobbyMemberPickResponse
    {
        public Guid Id { get; set; }
        public Guid LobbyMemberId { get; set; }
        public long PlayerId { get; set; }
        public long GamePk { get; set; }
        public DateTime Created { get; set; }
        public List<DrinkResponse> Drinks { get; set; } = new();
    }

    public class DrinkResponse
    {
        public Guid Id { get; set; }
        public Guid LobbyMemberPickId { get; set; }
        public Guid? RecipientLobbyMemberId { get; set; }
        public int EventId { get; set; }
    }

    public class JoinLobbyRequest
    {
        public string Name { get; set; } = null!;
        public bool IsBot { get; set; } = false;
        public BotPickStyle? BotPickStyle { get; set; }
    }

    public class MakePickRequest
    {
        public Guid? LobbyMemberId { get; set; }
        public long GamePk { get; set; }
        public long PlayerId { get; set; }
    }

    public class NewDrinkRequest
    {
        public Guid LobbyMemberPickId { get; set; }
        public int EventId { get; set; }
    }
}
