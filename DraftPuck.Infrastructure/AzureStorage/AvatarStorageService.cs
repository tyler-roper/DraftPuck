using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DraftPuck.Infrastructure.AzureStorage;
public class AvatarStorageService(BlobContainerClient container) : IAvatarStorageService
{
    public async Task<string> UploadBlobAsync(string blobName, Stream content, string contentType)
    {
        try
        {
            var blobClient = GetBlobClient(blobName);
            content.Seek(0, SeekOrigin.Begin);
            await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
            return blobClient.Uri.AbsoluteUri.Replace("host.docker.internal", "127.0.0.1");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during blob upload: {ex.Message}");
            throw;
        }
    }

    public async Task<Stream?> DownloadBlobAsync(string blobName)
    {
        try
        {
            var blobClient = GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                var download = await blobClient.DownloadContentAsync();
                return download.Value.Content.ToStream();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during blob download: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteBlobAsync(string blobName)
    {
        try
        {
            var blobClient = GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during blob deletion: {ex.Message}");
            throw;
        }
    }

    private BlobClient GetBlobClient(string blobName) => container.GetBlobClient(blobName);
}
