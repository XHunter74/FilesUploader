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

        var files = GetFiles(_appSettings.ScanFolder);
        _logger.LogInformation("Found {FileCount} files to upload.", files.Length);

        foreach (var file in files)
        {
            var filePath = Path.Combine(_appSettings.ScanFolder, file);
            try
            {
                _logger.LogInformation("Uploading file: {FileName}", file);
                await _azureStorageService.UploadFileAsync(
                    _appSettings.Container,
                    file,
                    File.ReadAllBytes(filePath),
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
                    var parts = x.Split(Path.DirectorySeparatorChar);
                    return parts.Length > 1 ? string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(1)) : x;
                })
                .ToArray();

            return files;
        }
        catch (Exception ex)
        {
            throw new IOException($"Error accessing files in {path} ", ex);
        }
    }
}
