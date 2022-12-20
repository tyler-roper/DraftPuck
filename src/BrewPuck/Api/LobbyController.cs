using Microsoft.EntityFrameworkCore;

namespace BrewPuck.Api
{
    public class LobbyController : BrewPuckApiControllerBase
    {
        private static Random random = new Random();
        private readonly BrewPuckContext _dbContext;

        public LobbyController(BrewPuckContext dbContext)
        {
            _dbContext = dbContext;
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
            var lobby = await _dbContext.Lobbies.FindAsync(id);

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

            if (!lobby.LobbyMembers.Any(lm => lm.UserId == CurrentUser.Id))
            {
                _dbContext.LobbyMembers.Add(new LobbyMember()
                {
                    LobbyId = lobby.Id,
                    UserId = CurrentUser.Id,
                    Name = name
                });
                await _dbContext.SaveChangesAsync();
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
