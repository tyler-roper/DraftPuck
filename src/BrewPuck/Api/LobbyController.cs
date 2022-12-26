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
                JoinCode = RandomString(4),
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLobbyById(Guid id)
        {
            var lobby = await _dbContext.Lobbies
                .Include(l => l.LobbyMembers)
                    .ThenInclude(lm => lm.LobbyMemberPicks)
                .FirstOrDefaultAsync(l => l.Id == id);

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

                _eventService.Notify(new LobbyEventModel()
                {
                    LobbyId = lobby.Id,
                    Type = LobbyEventType.UserJoined,
                    UserId = CurrentUser.Id,
                    Name = name,
                    LobbyMemberId = lobbyMember.Id
                });

            } else if (existingLobbyMember.Name != name)
            {
                //NAME CHANGE
                var lobbyMemberEntity = await _dbContext.LobbyMembers.FindAsync(existingLobbyMember.Id);
                lobbyMemberEntity.Name = name;
                await _dbContext.SaveChangesAsync();

                _eventService.Notify(new LobbyEventModel()
                {
                    LobbyId = lobby.Id,
                    Type = LobbyEventType.UserNameChanged,
                    UserId = CurrentUser.Id,
                    Name = name,
                    LobbyMemberId = existingLobbyMember.Id
                });
            }

            return Ok(lobby);
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
