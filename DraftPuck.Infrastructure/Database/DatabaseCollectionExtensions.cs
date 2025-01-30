using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Infrastructure.Database;
public static class DatabaseCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, string? connectionString) =>
        services.AddDbContext<DraftPuckContext>(options => options.UseSqlServer(connectionString));
}
