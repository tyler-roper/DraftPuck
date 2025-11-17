using DraftPuck.Application.Features.Achievements;
using DraftPuck.Application.Features.Achievements.Rules;
using DraftPuck.Application.Features.Users;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services
            .AddAchievementRules()
            .AddScoped<AchievementAwardService>()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateUserCommandHandler).Assembly));
    }

    private static IServiceCollection AddAchievementRules(this IServiceCollection services)
        => services.Scan(scan => scan
            .FromAssemblyOf<BaseAchievementRule>()
            .AddClasses(classes => classes.AssignableTo<IAchievementRule>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
}