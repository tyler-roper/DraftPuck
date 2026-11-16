using DraftPuck.Web.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using DraftPuck.Application.Common.Exceptions;
using DraftPuck.Application.Features.Auth;
using DraftPuck.Application.Features.Auth.PasswordReset;
using Microsoft.AspNetCore.Authorization;

namespace DraftPuck.Web.Features.Auth;

public class AuthController(IMediator mediator, ILogger<AuthController> logger) : BaseController()
{
    [EnableRateLimiting(RateLimits.Login)]
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

    /// <summary>Emails a reset link if the email belongs to an account. Responds the same either way.</summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.PasswordReset)]
    [HttpPost("passwordReset/request")]
    public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetRequestDto request)
    {
        await mediator.Send(new RequestPasswordResetCommand { Email = request.Email });
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Login)]
    [HttpPost("passwordReset/complete")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request)
    {
        await mediator.Send(new ResetPasswordCommand { Token = request.Token, Password = request.Password });
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var command = new LogoutCommand
        {
            UserId = CurrentUserId,
            RefreshToken = Request.Cookies[RefreshTokenName],
            IpAddress = GetIpAddress()
        };

        await mediator.Send(command);
        DeleteTokenCookie();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("refreshTokens/use")]
    public async Task<IActionResult> UseRefreshToken()
    {
        try
        {
            var refreshToken = Request.Cookies[RefreshTokenName];

            if (refreshToken == null)
                throw new UnauthorizedException("Refresh token is null.");

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
        catch (Exception ex)
        {
            // Users report being signed out; this says which way a refresh failed, and on what device. Never the tokens.
            logger.LogWarning("Refresh failed: {Reason} (cookie sent: {HasCookie}, CSRF header sent: {HasCsrfHeader}, user agent: {UserAgent})",
                ex.Message, Request.Cookies.ContainsKey(RefreshTokenName), Request.Headers.ContainsKey(AntiCsrfHeaderName),
                Request.Headers.UserAgent.ToString());
            throw new UnauthorizedException(ex.Message, ex);
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