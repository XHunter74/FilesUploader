using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace xhunter74.FilesUploader.Services;

public class AzureStorageService
{
    private readonly ILogger<AzureStorageService> _logger;
    private readonly string _connectionString;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureStorageService(
        ILogger<AzureStorageService> logger,
        string connectionString
        )
    {
        _logger = logger;
        _connectionString = connectionString;
        _blobServiceClient = new BlobServiceClient(_connectionString);
    }

    public async Task UploadFileAsync(string containerName, string blobName, byte[] sources, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting upload for file {BlobName} in folder {ContainerName}", blobName, containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);

            using var memoryStream = new MemoryStream(sources);
            await blobClient.UploadAsync(memoryStream, overwrite: true, cancellationToken: cancellationToken);

            _logger.LogInformation("Finished upload for file {BlobName} in folder {ContainerName}", blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while uploading file {BlobName} to folder {ContainerName}", blobName, containerName);
            throw;
        }
    }
}
