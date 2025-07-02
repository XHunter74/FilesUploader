using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace xhunter74.FilesUploader.Services;

/// <summary>
/// Provides methods to interact with Azure Blob Storage, including file uploads.
/// </summary>
public class AzureStorageService
{
    private readonly ILogger<AzureStorageService> _logger;
    private readonly string _connectionString;
    private readonly BlobServiceClient _blobServiceClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="connectionString">The Azure Blob Storage connection string.</param>
    public AzureStorageService(
        ILogger<AzureStorageService> logger,
        string connectionString
        )
    {
        _logger = logger;
        _connectionString = connectionString;
        _blobServiceClient = new BlobServiceClient(_connectionString);
    }

    /// <summary>
    /// Uploads a file to the specified Azure Blob Storage container.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="blobName">The name of the blob (file) to upload.</param>
    /// <param name="sources">The file content as a byte array.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous upload operation.</returns>
    /// <exception cref="Exception">Throws if the upload fails.</exception>
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
