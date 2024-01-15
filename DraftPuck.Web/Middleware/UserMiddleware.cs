using DraftPuck.Data.Data;

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
        string? userId = context.Request.Headers["user-id"].FirstOrDefault();
        if (userId == null || !Guid.TryParse(userId, out Guid id))
        {
            await _next(context);
            return;
        }

        User? user = await dbContext.Users.FindAsync(id);
        if (user != null)
        {
            context.Items["User"] = user;
        }

        await _next(context);
    }
}
