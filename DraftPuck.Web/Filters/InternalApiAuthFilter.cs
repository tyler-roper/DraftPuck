using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DraftPuck.Web.Filters;

public class InternalApiAuthFilter(IOptions<ApplicationOptions> options) : IAsyncActionFilter
{
    private readonly ApplicationOptions _options = options.Value;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expected = _options.InternalApiKey;
        const string headerName = "X-Internal-Api-Key";

        var request = context.HttpContext.Request;

        if (!request.Headers.TryGetValue(headerName, out var provided) || provided != expected)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
