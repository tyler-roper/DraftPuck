using DraftPuck.Application.Features.Discord;
using DraftPuck.Application.Features.Games;
using DraftPuck.Application.Features.Lobbies.Recap;
using DraftPuck.Application.Features.Users;
using DraftPuck.Infrastructure.Auth;
using DraftPuck.Infrastructure.AutoMapper;
using DraftPuck.Infrastructure.AzureStorage;
using DraftPuck.Infrastructure.Email;
using DraftPuck.Infrastructure.Firebase;
using DraftPuck.Infrastructure.Nhl;
using DraftPuck.Infrastructure.Nhl.MappingProfiles;
using DraftPuck.Infrastructure.Persistence;
using DraftPuck.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DraftPuck.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddNhlApi(configuration)
            .AddPersistence(configuration.GetConnectionString("DefaultConnection"))
            .AddFirebase(configuration)
            .AddRedisAndSignalR(configuration)
            .AddQueueServices(configuration)
            .AddBlobStorageServices(configuration)
            .AddEmail(configuration)
            .AddAutoMapperLicensed(configuration, typeof(UserMappingProfile).Assembly, typeof(GameMappingProfile).Assembly)
            .AddSingleton<IOAuthCache, RedisOAuthCache>()
            .AddSingleton<IGameCache, RedisGameCache>()
            .AddSingleton<ILobbyRecapCache, RedisLobbyRecapCache>()
            .AddTokenService(configuration);
    }
}