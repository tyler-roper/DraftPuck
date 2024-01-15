using DraftPuck.Data.Data;

namespace DraftPuck.Web.Api;

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
        User? user = await _dbContext.Users.FindAsync(id);
        return user == null
            ? NotFound()
            : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser()
    {
        User user = new();
        _ = _dbContext.Users.Add(user);
        _ = await _dbContext.SaveChangesAsync();

        return Created($"/user/{user.Id}", user);
    }
}
