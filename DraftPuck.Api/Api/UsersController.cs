namespace DraftPuck.Api.Api
{
    public class UsersController : DraftPuckApiControllerBase
    {
        private readonly DraftPuckContext _dbContext;

        public UsersController(DraftPuckContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            return user == null
                ? NotFound()
                : Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser()
        {
            var user = new User();
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return Created($"/user/{user.Id}", user);
        }
    }
}
