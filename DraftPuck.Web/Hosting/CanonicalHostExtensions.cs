using DraftPuck.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace DraftPuck.Web.Hosting;

public static class CanonicalHostExtensions
{
    /// <summary>
    /// Sends www.&lt;site&gt; to the site's own host (from Application:BasePath) with a permanent redirect. The two hosts
    /// have separate cookies, so a session on one doesn't exist on the other, and push notification links always use
    /// the BasePath host. Other host names (health probes, Azure's internal address) are left alone.
    /// </summary>
    public static IApplicationBuilder UseCanonicalHost(this IApplicationBuilder app)
    {
        var basePath = app.ApplicationServices.GetRequiredService<IOptions<ApplicationOptions>>().Value.BasePath;
        if (!Uri.TryCreate(basePath, UriKind.Absolute, out var canonical))
            return app;

        var wwwHost = "www." + canonical.Host;
        return app.Use(async (context, next) =>
        {
            if (context.Request.Host.Host.Equals(wwwHost, StringComparison.OrdinalIgnoreCase))
            {
                var target = new UriBuilder(canonical) { Path = context.Request.Path, Query = context.Request.QueryString.Value }.Uri;
                context.Response.Redirect(target.AbsoluteUri, permanent: true);
                return;
            }

            await next();
        });
    }
}
