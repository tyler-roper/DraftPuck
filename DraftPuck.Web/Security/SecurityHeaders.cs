namespace DraftPuck.Web.Security;

/// <summary>
/// Response headers that limit what a page can do if something slips past escaping. The JWT lives in localStorage,
/// so the Content-Security-Policy's script rules are the main line of defence against an injected script reading it.
/// </summary>
public static class SecurityHeaders
{
    /// <summary>
    /// Scripts only from this site (the app, env.js) and Firebase's CDN, which the service worker loads with
    /// importScripts. No inline scripts, eval, plugins, framing or foreign form posts. Images and API calls stay open
    /// (team logos, avatars in blob storage, Firebase's googleapis endpoints), since they can't run code.
    /// </summary>
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' https://www.gstatic.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' data: https://fonts.gstatic.com; " +
        "img-src 'self' data: blob: https:; " +
        "connect-src 'self' https: wss:; " +
        "worker-src 'self'; " +
        "manifest-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    /// <summary>
    /// Adds the headers to every response, static files included. Not in Development: there the page comes from the
    /// Vite dev server, which needs inline scripts and styles for hot reload.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return app;

        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                return Task.CompletedTask;
            });

            await next();
        });
    }
}
