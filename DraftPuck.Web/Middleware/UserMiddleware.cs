using DraftPuck.Infrastructure.Database;

namespace DraftPuck.Web.Middleware;

public class UserMiddleware
{
    private readonly RequestDelegate _next;

    public UserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DraftPuckContext dbContext)
    {
        var userId = context.Request.Headers["user-id"].FirstOrDefault();
        if (userId == null || !Guid.TryParse(userId, out var id))
        {
            await _next(context);
            return;
        }

        var user = await dbContext.Users.FindAsync(id);
        if (user != null)
        {
            context.Items["User"] = user;
        }

        await _next(context);
    }
}
