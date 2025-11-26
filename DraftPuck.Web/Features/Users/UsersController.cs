using Azure;
using DraftPuck.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;

namespace DraftPuck.Web.Features.Users;

public class UsersController(IMediator mediator) : BaseController()
{
    [HttpGet]
    public async Task<ActionResult<UserDto>> GetUserByName([FromQuery] string name)
    {
        if (string.IsNullOrEmpty(name))
            return BadRequest("User name is required for lookup.");

        var query = new GetUserByNameQuery { Name = name };
        var userDto = await mediator.Send(query);
        return Ok(userDto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var query = new GetUserByIdQuery { Id = id };
        var userDto = await mediator.Send(query);
        return Ok(userDto);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CreateGuest()
    {
        var authenticationResultDto = await mediator.Send(new CreateGuestCommand() { IpAddress = GetIpAddress() });
        SetTokenCookie(authenticationResultDto.RefreshToken);
        return Created($"/user/{authenticationResultDto.User.Id}", authenticationResultDto);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequestDto request)
    {
        var command = new UpdateUserCommand()
        {
            TargetUserId = id,
            RequesterUserId = CurrentUserId,
            RequesterIsAuthenticated = IsSignedIn,
            Email = request.Email,
            Nickname = request.Nickname,
            FcmRegistrationToken = request.FcmRegistrationToken,
            DrinkReceivedNotificationPreference = request.DrinkReceivedNotificationPreference,
            DrinkAwardedNotificationPreference = request.DrinkAwardedNotificationPreference,
            ChatNotificationPreference = request.ChatNotificationPreference,
            PickingStartedNotificationPreference = request.PickingStartedNotificationPreference,
            AchievementAwardedNotificationPreference = request.AchievementAwardedNotificationPreference,
            BannerId = request.BannerId,
            TitleId = request.TitleId,
            Password = request.Password,
            AvatarData = request.AvatarData
        };

        var updatedUserDto = await mediator.Send(command);
        return Ok(updatedUserDto);
    }

    [HttpPost("signup")]
    public async Task<IActionResult> CreateAccount(CreateUserRequestDto request)
    {
        var command = new CreateUserCommand()
        {
            GuestUserId = CurrentUserId,
            Email = request.Email,
            Nickname = request.Nickname,
            Password = request.Password
        };

        var userDto = await mediator.Send(command);
        return Created($"/user/{userDto.Id}", userDto);
    }
}
