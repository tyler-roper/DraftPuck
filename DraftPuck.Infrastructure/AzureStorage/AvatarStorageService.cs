using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace DraftPuck.Infrastructure.AzureStorage;

/// <summary>Avatar blobs in Azure Storage. Failures propagate; callers decide whether they're fatal and log them.</summary>
public class AvatarStorageService(BlobContainerClient container) : IAvatarStorageService
{
    public async Task<string> UploadBlobAsync(string blobName, Stream content, string contentType)
    {
        var blobClient = GetBlobClient(blobName);
        content.Seek(0, SeekOrigin.Begin);
        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        return blobClient.Uri.AbsoluteUri.Replace("host.docker.internal", "127.0.0.1");
    }

    public async Task<Stream?> DownloadBlobAsync(string blobName)
    {
        var blobClient = GetBlobClient(blobName);
        if (!await blobClient.ExistsAsync())
            return null;

        var download = await blobClient.DownloadContentAsync();
        return download.Value.Content.ToStream();
    }

    public async Task DeleteBlobAsync(string blobName) =>
        await GetBlobClient(blobName).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

    public async Task DeleteBlobByUriAsync(string fullUri) =>
        await DeleteBlobAsync(new BlobUriBuilder(new Uri(fullUri)).BlobName);

    private BlobClient GetBlobClient(string blobName) => container.GetBlobClient(blobName);
}
