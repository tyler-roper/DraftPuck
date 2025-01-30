namespace DraftPuck.Web.Api;

public class UsersController : DraftPuckApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user == null
            ? NotFound()
            : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser()
    {
        var user = await _userService.CreateUserAsync();
        return Created($"/user/{user.Id}", user);
    }

    [HttpPatch("{id}/fcmtoken")]
    public async Task<IActionResult> UpdateFcmRegistrationToken(Guid id, UpdateFcmRegistrationTokenRequestModel model)
    {
        var user = await _userService.UpdateFcmRegistrationTokenAsync(id, model);
        return user == null
            ? NotFound()
            : Ok(user);
    }
}
