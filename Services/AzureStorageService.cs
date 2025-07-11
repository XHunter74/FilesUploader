using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using xhunter74.FilesUploader.Models;

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

            _logger.LogDebug("Getting BlobContainerClient for container: {ContainerName}", containerName);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            _logger.LogDebug("Ensuring container exists: {ContainerName}", containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            _logger.LogDebug("Getting BlobClient for blob: {BlobName}", blobName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogDebug("Uploading blob: {BlobName} (size: {Size} bytes)", blobName, sources?.Length ?? 0);
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

    /// <summary>
    /// Uploads a file from the specified file path to the given Azure Blob Storage container.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="blobName">The name of the blob (file) to upload.</param>
    /// <param name="filePath">The local file path of the file to upload.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous upload operation.</returns>
    /// <exception cref="Exception">Throws if the upload fails.</exception>
    public async Task UploadFileAsync(string containerName, string blobName, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting upload for file '{BlobName}' from path '{FilePath}' to container '{ContainerName}'", blobName, filePath, containerName);

            _logger.LogDebug("Getting BlobContainerClient for container: '{ContainerName}'", containerName);
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            _logger.LogDebug("Ensuring container exists: '{ContainerName}'", containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            _logger.LogDebug("Getting BlobClient for blob: '{BlobName}'", blobName);
            var blobClient = containerClient.GetBlobClient(blobName);

            _logger.LogDebug("Uploading blob: '{BlobName}' from file: '{FilePath}'", blobName, filePath);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken: cancellationToken);

            _logger.LogInformation("Finished upload for file '{BlobName}' from path '{FilePath}' to container '{ContainerName}'", blobName, filePath, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while uploading file '{BlobName}' from path '{FilePath}' to container '{ContainerName}'", blobName, filePath, containerName);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a list of files (blobs) in the specified Azure Blob Storage container.
    /// </summary>
    /// <param name="containerName">The name of the blob container.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AppFileInfo"/> objects representing the blobs.</returns>
    /// <exception cref="Exception">Throws if the listing fails.</exception>
    public async Task<IEnumerable<AppFileInfo>> GetFilesInContainerAsync(string containerName, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Listing blobs in container '{ContainerName}'", containerName);

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobNames = new List<AppFileInfo>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                blobNames.Add(new AppFileInfo
                {
                    Name = Path.GetFileName(blobItem.Name),
                    Folder = Path.GetDirectoryName(blobItem.Name) ?? string.Empty,
                    Created = blobItem.Properties.CreatedOn ?? DateTimeOffset.MinValue // Use a default value if null.
                });
            }

            _logger.LogInformation("Found {Count} blobs in container '{ContainerName}'", blobNames.Count, containerName);
            return blobNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while listing blobs in container '{ContainerName}'", containerName);
            throw;
        }
    }

    /// <summary>
    /// Deletes the specified files (blobs) from the given Azure Blob Storage container.
    /// </summary>
    /// <param name="container">The name of the blob container.</param>
    /// <param name="filesForDelete">A collection of blob names to delete.</param>
    /// <param name="cancellationToken">A cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <exception cref="Exception">Throws if the deletion fails.</exception>
    public async Task DeleteFilesAsync(string container, IEnumerable<string> filesForDelete, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting deletion of {Count} files from container '{ContainerName}'", filesForDelete.Count(), container);

            var containerClient = _blobServiceClient.GetBlobContainerClient(container);

            foreach (var fileName in filesForDelete)
            {
                _logger.LogInformation("Deleting blob: {BlobName} from container: '{ContainerName}'", fileName, container);
                var blobClient = containerClient.GetBlobClient(fileName);
                await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Finished deletion of files from container '{ContainerName}'", container);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting files from container '{ContainerName}'", container);
            throw;
        }
    }
}
