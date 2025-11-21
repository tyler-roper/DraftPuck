namespace DraftPuck.Shared.Common;
public interface IStorageService
{
    Task<string> UploadBlobAsync(string blobName, Stream content, string contentType);
    Task<Stream?> DownloadBlobAsync(string blobName);
    Task DeleteBlobAsync(string blobName);
    Task DeleteBlobByUriAsync(string fullUri);
}