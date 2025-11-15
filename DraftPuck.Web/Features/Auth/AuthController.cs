using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;

namespace DraftPuck.Web.Features.Auth;

public class AuthController(IMediator mediator) : BaseController()
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var command = new LoginCommand()
        {
            Email = request.Email,
            Password = request.Password,
            IpAddress = GetIpAddress(),
            GuestUserId = CurrentUserId
        };

        var response = await mediator.Send(command);
        SetTokenCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var command = new LogoutCommand
        {
            UserId = CurrentUserId,
            IpAddress = GetIpAddress()
        };

        await mediator.Send(command);
        DeleteTokenCookie();
        return NoContent();
    }

    [HttpPost("refreshTokens/use")]
    public async Task<IActionResult> UseRefreshToken()
    {
        try
        {
            var refreshToken = Request.Cookies[RefreshTokenName];

            if (refreshToken == null)
                return Unauthorized();

            var command = new UseRefreshTokenCommand
            {
                RefreshToken = refreshToken,
                IpAddress = GetIpAddress(),
                AntiCsrfTokenHeader = GetAntiCsrfTokenHeader()
            };

            var response = await mediator.Send(command);
            SetTokenCookie(response.RefreshToken);
            return Ok(response);
        }
        catch
        {
            return Unauthorized();
        }
    }

    [Authorize]
    [HttpPost("refreshTokens/revoke")]
    public async Task<IActionResult> RevokeToken(RevokeTokenRequestDto request)
    {
        var token = request.Token ?? Request.Cookies[RefreshTokenName] ?? throw new BadRequestException("Missing token.");
        var command = new RevokeTokenCommand()
        {
            Token = token,
            IpAddress = GetIpAddress()
        };

        try
        {
            await mediator.Send(command);
            return NoContent();
        }
        catch
        {
            return BadRequest();
        }
    }
}