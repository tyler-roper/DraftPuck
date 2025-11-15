using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.AzureStorage;

public static class AzureStorageServiceCollectionExtensions
{
    private static readonly string DevelopmentConnectionString =
        $"DefaultEndpointsProtocol=http;" +
        "AccountName=devstoreaccount1;" +
        "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
        "BlobEndpoint=http://host.docker.internal:10000/devstoreaccount1;" +
        "QueueEndpoint=http://host.docker.internal:10001/devstoreaccount1;" +
        "TableEndpoint=http://host.docker.internal:10002/devstoreaccount1;";

    public static IServiceCollection AddQueueServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AzureStorageOptions>(config.GetSection(AzureStorageOptions.SectionName));

        // Achievement queue
        services.AddSingleton<IAchievementQueueService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
            var connectionString = options.UseDevelopmentStorage
                ? DevelopmentConnectionString
                : options.ConnectionString;

            var client = new QueueClient(connectionString, options.AchievementQueueName, new() { MessageEncoding = QueueMessageEncoding.Base64 });
            client.CreateIfNotExists();

            return new AchievementQueueService(client);
        });

        // NHL queue
        services.AddSingleton<INhlQueueService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
            var connectionString = options.UseDevelopmentStorage
                ? DevelopmentConnectionString
                : options.ConnectionString;

            var client = new QueueClient(connectionString, options.NhlQueueName, new() { MessageEncoding = QueueMessageEncoding.Base64 });
            client.CreateIfNotExists();
            return new NhlQueueService(client);
        });

        return services;
    }

    public static IServiceCollection AddBlobStorageServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AzureStorageOptions>(config.GetSection(AzureStorageOptions.SectionName));
        services.AddSingleton<IAvatarStorageService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
            return new AvatarStorageService(GetContainerClient(options, options.AvatarStorageContainer));
        });

        return services;
    }

    private static BlobContainerClient GetContainerClient(AzureStorageOptions options, string containerName)
    {
        var connectionString = options.UseDevelopmentStorage
            ? DevelopmentConnectionString
            : options.ConnectionString;

        var client = new BlobServiceClient(connectionString);
        var containerClient = client.GetBlobContainerClient(containerName);
        containerClient.CreateIfNotExists();

        return containerClient;
    }
}