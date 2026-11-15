using DraftPuck.Shared.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DraftPuck.Infrastructure.AutoMapper;

public static class AutoMapperCollectionExtensions
{
    /// <summary>AutoMapper with the Lucky Penny license key (shared with MediatR, see <see cref="LuckyPennyOptions"/>).</summary>
    public static IServiceCollection AddAutoMapperLicensed(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
    {
        var licenseKey = configuration[LuckyPennyOptions.LicenseKey];
        return services.AddAutoMapper(cfg => cfg.LicenseKey = licenseKey, assemblies);
    }
}
