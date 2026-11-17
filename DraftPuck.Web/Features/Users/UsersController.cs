using DraftPuck.Web.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting(RateLimits.GuestCreation)]
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
            BannerId = request.BannerId,
            TitleId = request.TitleId,
            Password = request.Password,
            CurrentPassword = request.CurrentPassword,
            AvatarData = request.AvatarData
        };

        var updatedUserDto = await mediator.Send(command);
        return Ok(updatedUserDto);
    }

    /// <summary>Sets the caller's default notification settings, optionally applying them to their active lobbies too.</summary>
    [HttpPut("me/notificationPreferences")]
    public async Task<ActionResult<UserDto>> UpdateNotificationPreferences(UpdateNotificationPreferencesRequestDto request)
    {
        var user = await mediator.Send(new UpdateNotificationPreferencesCommand
        {
            UserId = CurrentUserId,
            DrinkReceivedNotificationPreference = request.DrinkReceivedNotificationPreference,
            DrinkAwardedNotificationPreference = request.DrinkAwardedNotificationPreference,
            ChatNotificationPreference = request.ChatNotificationPreference,
            PickingStartedNotificationPreference = request.PickingStartedNotificationPreference,
            AchievementAwardedNotificationPreference = request.AchievementAwardedNotificationPreference,
            RecapNotificationPreference = request.RecapNotificationPreference,
            ApplyToActiveLobbies = request.ApplyToActiveLobbies
        });
        return Ok(user);
    }

    /// <summary>Registers the device the caller is on (its push token), so it gets their push notifications.</summary>
    [HttpPost("me/devices")]
    public async Task<IActionResult> RegisterDevice(DeviceRequestDto request)
    {
        await mediator.Send(new RegisterDeviceCommand
        {
            UserId = CurrentUserId,
            Token = request.Token,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IsInstalledApp = request.IsInstalledApp
        });
        return NoContent();
    }

    /// <summary>Stops the given device getting the caller's push notifications (on sign-out).</summary>
    [HttpPost("me/devices/forget")]
    public async Task<IActionResult> ForgetDevice(DeviceRequestDto request)
    {
        await mediator.Send(new ForgetDeviceCommand { UserId = CurrentUserId, Token = request.Token });
        return NoContent();
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
