using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Infrastructure.Persistence;
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string? connectionString)
    {
        return services
            .AddDbContext<DraftPuckContext>(options => options.UseSqlServer(connectionString))
            .AddScoped<IDbContext>(provider => provider.GetRequiredService<DraftPuckContext>())
            .AddScoped<IUserRepository, UserRepository>();
    }
}
