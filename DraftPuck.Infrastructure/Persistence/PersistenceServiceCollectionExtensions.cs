using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Infrastructure.Persistence;
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string? connectionString)
    {
        return services
            .AddDbContext<DraftPuckContext>(options => options.UseSqlServer(connectionString,
                // Queries that include several collections run as one query unless they say AsSplitQuery. Saying so
                // explicitly stops EF warning about it on every such query.
                sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)))
            .AddScoped<IDbContext>(provider => provider.GetRequiredService<DraftPuckContext>())
            .AddScoped<IUserRepository, UserRepository>();
    }
}
