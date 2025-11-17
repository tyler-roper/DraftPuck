using DraftPuck.Application.Features.Games;
using DraftPuck.Infrastructure.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.Nhl;
public static class NhlServiceCollectionExtensions
{
    public static IServiceCollection AddNhlApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NhlOptions>(configuration.GetSection(NhlOptions.SectionName));

        services.AddHttpClient<INhlClient, NhlClient>(client =>
        {
            client.BaseAddress = new Uri("https://api-web.nhle.com/v1/");
        });

        services.AddSingleton<IApiRateLimiter>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<NhlOptions>>().Value;
            return new ApiRateLimiter(options.DelayInMilliseconds);
        });

        return services;
    }
}