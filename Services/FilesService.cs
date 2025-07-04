using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using xhunter74.CollectionManager.API.Settings;

namespace xhunter74.FilesUploader.Services;

/// <summary>
/// Provides file scanning and uploading services to Azure Storage.
/// </summary>
public class FilesService
{
    private readonly ILogger<FilesService> _logger;
    private readonly AppSettings _appSettings;
    private readonly AzureStorageService _azureStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="azureStorageService">The Azure storage service.</param>
    /// <param name="options">The application settings options.</param>
    public FilesService(
        ILogger<FilesService> logger,
        AzureStorageService azureStorageService,
        IOptions<AppSettings> options
        )
    {
        _logger = logger;
        _appSettings = options.Value;
        _azureStorageService = azureStorageService;
        _logger.LogInformation("FilesService initialized with scan folder: {ScanFolder}", _appSettings.ScanFolder);
    }

    /// <summary>
    /// Scans the configured folder and uploads all files to Azure Storage, then deletes them locally.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task ScanFolderAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting folder scan in: {ScanFolder}", _appSettings.ScanFolder);
        if (!Directory.Exists(_appSettings.ScanFolder))
        {
            Directory.CreateDirectory(_appSettings.ScanFolder);
            _logger.LogWarning("Scan folder did not exist. Created: {ScanFolder}", _appSettings.ScanFolder);
        }

        var fullPath = Path.GetFullPath(_appSettings.ScanFolder);

        var files = GetFiles(fullPath);
        _logger.LogInformation("Found {FileCount} files to upload.", files.Length);

        foreach (var file in files)
        {
            var filePath = Path.Combine(fullPath, file);
            try
            {
                _logger.LogInformation("Uploading file: {FileName}", file);
                await _azureStorageService.UploadFileAsync(
                    _appSettings.Container,
                    file,
                    filePath,
                    cancellationToken);
                _logger.LogInformation("Successfully uploaded file: {FileName}", file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while uploading file {FileName} to Azure Storage", file);
            }
            finally
            {
                try
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted local file: {FileName}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete local file: {FileName}", file);
                }
            }
        }
        _logger.LogInformation("Folder scan and upload process completed.");
    }

    /// <summary>
    /// Gets all files in the specified directory and its subdirectories.
    /// </summary>
    /// <param name="path">The directory path to scan.</param>
    /// <returns>An array of file paths relative to the scan folder.</returns>
    /// <exception cref="IOException">Thrown if there is an error accessing the files.</exception>
    private static string[] GetFiles(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            files = files
                .Select(x =>
                {
                    var filePath = x.Replace($"{path}{Path.DirectorySeparatorChar}", "");
                    return filePath;
                })
                .ToArray();

            return files;
        }
        catch (Exception ex)
        {
            throw new IOException($"Error accessing files in {path} ", ex);
        }
    }

    /// <summary>
    /// Cleans up outdated files in the configured Azure Storage container by deleting files
    /// that exceed the maximum number of files to store per folder, as specified in <see cref="AppSettings.MaxFilesToStore"/>.
    /// For each folder in the container, only the most recent files up to the limit are kept; older files are deleted.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <remarks>
    /// This method retrieves all files in the container, groups them by folder, and deletes the oldest files
    /// if the number of files in a folder exceeds the configured maximum. If <see cref="AppSettings.MaxFilesToStore"/>
    /// is not set, the method returns without performing any action.
    /// </remarks>
    public async Task CleanOutdatedFilesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cleanup of outdated files in container: {Container}", _appSettings.Container);
        if (!_appSettings.MaxFilesToStore.HasValue)
            return;

        var maxFilesToStore = _appSettings.MaxFilesToStore.Value;

        _logger.LogInformation("Max files to store: {MaxFilesToStore}", maxFilesToStore);

        var files = await _azureStorageService.GetFilesInContainerAsync(_appSettings.Container, cancellationToken);

        var folders = files
            .Select(f => f.Folder ?? string.Empty)
            .Distinct()
            .ToList();

        var filesForDelete = new List<string>();

        foreach (var folder in folders)
        {
            var filesCount = files.Where(e => e.Folder == folder).Count();
            if (filesCount > maxFilesToStore)
            {
                var forDelete = files
                    .Where(e => e.Folder == folder)
                    .OrderByDescending(e => e.Created)
                    .Skip(maxFilesToStore)
                    .Select(e => Path.Combine(e.Folder, e.Name))
                    .ToList();
                filesForDelete.AddRange(forDelete);
            }
        }

        if (filesForDelete.Count == 0)
        {
            _logger.LogInformation("No outdated files to delete.");
            return;
        }

        _logger.LogInformation("Found {Count} outdated files to delete.", filesForDelete.Count);

        await _azureStorageService
            .DeleteFilesAsync(_appSettings.Container, filesForDelete, cancellationToken);

        _logger.LogInformation("Cleanup of outdated files completed.");
    }
}
