using DraftPuck.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DraftPuck.Web.Features;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class BaseController() : ControllerBase
{
    protected readonly string RefreshTokenName = "draftpuck.refreshtoken";
    protected readonly string AntiCsrfHeaderName = "CSRF-Token";

    private (Guid Id, bool IsAuthenticated)? _userIdentificationResult;

    private (Guid Id, bool IsAuthenticated) GetIdentityResult()
    {
        if (_userIdentificationResult.HasValue)
            return _userIdentificationResult.Value;

        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var claimUserId))
        {
            var isGuest = User.Claims.Any(c => c.Type == "guest" && c.Value == "true");
            _userIdentificationResult = (claimUserId, !isGuest);
            return _userIdentificationResult.Value;
        }

        throw new UnauthorizedException("User ID could not be determined from token.");
    }

    protected Guid CurrentUserId => GetIdentityResult().Id;
    protected bool IsSignedIn => GetIdentityResult().IsAuthenticated;

    protected string? GetAntiCsrfTokenHeader()
    {
        if (!Request.Headers.TryGetValue(AntiCsrfHeaderName, out var antiCsrfTokenHeaderString))
            throw new UnauthorizedException("Missing anti-CSRF header.");

        return antiCsrfTokenHeaderString;
    }

    protected void SetTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(14),
            Secure = true,
            SameSite = SameSiteMode.Lax
        };
        Response.Cookies.Append(RefreshTokenName, refreshToken, cookieOptions);
    }

    protected void DeleteTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenName);
    }

    protected string GetIpAddress()
    {
        return Request.Headers.TryGetValue("X-Forwarded-For", out var value)
            ? (string)value!
            : HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();
    }
}