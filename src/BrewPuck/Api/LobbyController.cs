using BrewPuck.Data;
using Microsoft.EntityFrameworkCore;

namespace BrewPuck.Api
{
    public class LobbyController : BrewPuckApiControllerBase
    {
        private static Random random = new Random();
        private readonly BrewPuckContext _dbContext;
        private readonly IEventService _eventService;

        public LobbyController(BrewPuckContext dbContext, IEventService eventService)
        {
            _dbContext = dbContext;
            _eventService = eventService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLobby(string name)
        {
            if (CurrentUser == null) return Unauthorized();
            if (string.IsNullOrEmpty(name)) return BadRequest();

            var newLobbyId = Guid.NewGuid();
            var lobby = new Lobby()
            {
                Id = newLobbyId,
                JoinCode = await RandomString(4),
                Status = 0,
                CreatedBy = CurrentUser.Id
            };

            _dbContext.Lobbies.Add(lobby);
            _dbContext.LobbyMembers.Add(new LobbyMember()
            {
                LobbyId = newLobbyId,
                UserId = CurrentUser.Id,
                Name = name
            });

            await _dbContext.SaveChangesAsync();
            return Created($"lobbies/{newLobbyId}", lobby);
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetLobbyByCode(string code)
        {
            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                    .ThenInclude(lm => lm.LobbyMemberPicks)
                .FirstOrDefaultAsync(l => l.JoinCode == code);

            return lobby == null
                ? NotFound()
                : Ok(lobby);
        }

        [HttpPost("join/{code}")]
        public async Task<IActionResult> JoinLobbyByCode(string code, string name)
        {
            if (CurrentUser == null) return Unauthorized();

            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                .FirstOrDefaultAsync(l => l.JoinCode == code);
            if (lobby == null) return NotFound();

            var existingLobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == CurrentUser.Id);
            if (existingLobbyMember == null)
            {
                //JOINED LOBBY
                var lobbyMember = new LobbyMember()
                {
                    LobbyId = lobby.Id,
                    UserId = CurrentUser.Id,
                    Name = name
                };
                _dbContext.LobbyMembers.Add(lobbyMember);
                await _dbContext.SaveChangesAsync();

                _eventService.Notify(new LobbyEventModel(LobbyEventType.UserJoined, lobby.Id, lobbyMember));

            } else if (existingLobbyMember.Name != name)
            {
                //NAME CHANGE
                var lobbyMemberEntity = await _dbContext.LobbyMembers.FindAsync(existingLobbyMember.Id);
                lobbyMemberEntity.Name = name;
                await _dbContext.SaveChangesAsync();

                _eventService.Notify(new LobbyEventModel(LobbyEventType.UserNameChanged, lobby.Id, lobbyMemberEntity));
            }

            return Ok(lobby);
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

            var lobbyMember = lobby.LobbyMembers.FirstOrDefault(lm => lm.UserId == CurrentUser.Id);
            if (lobbyMember == null) return Unauthorized();

            var player = await _dbContext.Players.FindAsync(request.Player.Id);
            if (player == null) {
                player = new Player()
                {
                    Id = request.Player.Id,
                    FirstName = request.Player.FirstName,
                    LastName = request.Player.LastName,
                    Position = request.Player.Position,
                    Number = request.Player.Number,
                    TeamId = request.Player.TeamId
                };

                _dbContext.Players.Add(player);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                player.FirstName = request.Player.FirstName;
                player.LastName = request.Player.LastName;
                player.Position = request.Player.Position;
                player.Number = request.Player.Number;
                player.TeamId = request.Player.TeamId;
                await _dbContext.SaveChangesAsync();
            }

            var pick = new LobbyMemberPick()
            {
                LobbyMemberId = lobbyMember.Id,
                PlayerId = player.Id,
                GamePk = request.GamePk
            };

            _dbContext.LobbyMemberPicks.Add(pick);
            await _dbContext.SaveChangesAsync();

            _eventService.Notify(new LobbyEventModel(LobbyEventType.NewPick, lobby.Id, lobbyMember));

            return Ok(pick);
        }

        [HttpPost("pick/{pickId}/score")]
        public async Task<IActionResult> PickScored(Guid pickId, [FromQuery] int eventId)
        {

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

    public class MakePickRequest
    {
        public long GamePk { get; set; }
        public PlayerRequest Player { get; set; } = null!;
    }

    public class PlayerRequest
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Number { get; set; }
        public int TeamId { get; set; }
        public string Position { get; set; } = null!;
    }
}
