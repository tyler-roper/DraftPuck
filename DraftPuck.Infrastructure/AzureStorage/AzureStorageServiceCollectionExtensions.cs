using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DraftPuck.Infrastructure.AzureStorage;

public static class AzureStorageServiceCollectionExtensions
{
    public static IServiceCollection AddQueueServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AzureStorageOptions>(config.GetSection(AzureStorageOptions.SectionName));

        // Achievement queue
        services.AddSingleton<IAchievementQueueService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
            if (string.IsNullOrEmpty(options.ConnectionString))
                throw new InvalidOperationException("Azure Storage connection string is required but missing.");

            var client = new QueueClient(options.ConnectionString, options.AchievementQueueName, new() { MessageEncoding = QueueMessageEncoding.Base64 });
            client.CreateIfNotExists();

            return new AchievementQueueService(client);
        });

        // NHL queue
        services.AddSingleton<INhlQueueService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;
            if (string.IsNullOrEmpty(options.ConnectionString))
                throw new InvalidOperationException("Azure Storage connection string is required but missing.");

            var client = new QueueClient(options.ConnectionString, options.NhlQueueName, new() { MessageEncoding = QueueMessageEncoding.Base64 });
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
            var containerClient = GetContainerClient(options, options.AvatarStorageContainer);
            containerClient.SetAccessPolicy(PublicAccessType.Blob);
            return new AvatarStorageService(containerClient);
        });

        return services;
    }

    private static BlobContainerClient GetContainerClient(AzureStorageOptions options, string containerName)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
            throw new InvalidOperationException("Azure Storage connection string is required but missing.");

        var client = new BlobServiceClient(options.ConnectionString);
        var containerClient = client.GetBlobContainerClient(containerName);
        containerClient.CreateIfNotExists();
        return containerClient;
    }
}