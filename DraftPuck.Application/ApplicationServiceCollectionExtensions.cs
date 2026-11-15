using DraftPuck.Application.Common.Options;
using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Achievements.Rules;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Users;
using DraftPuck.Domain.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddAchievementRules()
            .AddScoped<AchievementAwardService>()
            .AddScoped<GameRosterSync>()
            .AddMediatR(cfg =>
            {
                // The Lucky Penny bundle key, shared with AutoMapper.
                cfg.LicenseKey = configuration[LuckyPennyOptions.LicenseKey];
                cfg.RegisterServicesFromAssembly(typeof(CreateUserCommandHandler).Assembly);
            });
    }

    private static IServiceCollection AddAchievementRules(this IServiceCollection services)
        => services.Scan(scan => scan
            .FromAssemblyOf<BaseAchievementRule>()
            .AddClasses(classes => classes.AssignableTo<IAchievementRule>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
}