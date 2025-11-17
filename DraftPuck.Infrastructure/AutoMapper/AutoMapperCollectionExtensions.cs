using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DraftPuck.Infrastructure.AutoMapper;

public static class JwtAuthCollectionExtensions
{
    public static IServiceCollection AddAutoMapperLicensed(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
    {
        return services
            .Configure<AutoMapperOptions>(configuration.GetSection(AutoMapperOptions.SectionName))
            .AddAutoMapper((serviceProvider, cfg) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AutoMapperOptions>>().Value;
                cfg.LicenseKey = options.LicenseKey;
            }, assemblies
            );
    }
}

