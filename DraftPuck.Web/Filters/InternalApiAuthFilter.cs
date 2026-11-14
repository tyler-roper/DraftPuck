using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DraftPuck.Web.Filters;

/// <summary>
/// Lets a request through only when it carries the configured internal API key (the Discord bot's calls).
/// With no key configured, nothing gets through: an unset key must never match an empty header.
/// </summary>
public class InternalApiAuthFilter(IOptions<ApplicationOptions> options) : IAsyncActionFilter
{
    public const string HeaderName = "X-Internal-Api-Key";

    private readonly ApplicationOptions _options = options.Value;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!IsAuthorized(context.HttpContext.Request.Headers[HeaderName], _options.InternalApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }

    public static bool IsAuthorized(string? provided, string? expected)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided))
            return false;

        // Constant-time, so response timing doesn't reveal how much of a guessed key was right.
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
    }
}
