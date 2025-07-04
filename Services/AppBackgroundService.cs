using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using xhunter74.CollectionManager.API.Settings;

namespace xhunter74.FilesUploader.Services;

/// <summary>
/// Background service that periodically scans a folder and uploads files using <see cref="FilesService"/>.
/// </summary>
public class AppBackgroundService : BaseBackgroundService
{
    private readonly SemaphoreSlim _taskSemaphore = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppSettings _appSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppBackgroundService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging service events.</param>
    /// <param name="scopeFactory">The service scope factory for dependency injection.</param>
    /// <param name="options">The application settings options.</param>
    public AppBackgroundService(
        ILogger<AppBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<AppSettings> options
    ) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _appSettings = options.Value;
    }

    /// <summary>
    /// Periodically scans the configured folder and uploads files using <see cref="FilesService"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that is triggered when the service is stopping.</param>
    /// <returns>A task that represents the background execution.</returns>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation($"{nameof(AppBackgroundService)} is starting.");

        cancellationToken.Register(() =>
            Logger.LogInformation($"{nameof(AppBackgroundService)} task is stopping."));

        while (!cancellationToken.IsCancellationRequested)
        {
            await _taskSemaphore.WaitAsync(cancellationToken);
            Logger.LogInformation("=========================== SCAN STARTED ===========================");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var filesService = scope.ServiceProvider.GetService<FilesService>();
                await filesService.ScanFolderAsync(cancellationToken);
                await filesService.CleanOutdatedFilesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{GetType().Name}.{nameof(ExecuteAsync)} -> Exception occurred:");
            }
            finally
            {
                _taskSemaphore.Release();
            }

            Logger.LogInformation("=========================== SCAN FINISHED ==========================");
            await Task.Delay(TimeSpan.FromMinutes(_appSettings.ScanIntervalMinutes), cancellationToken);
        }

        Logger.LogInformation($"{nameof(AppBackgroundService)} background task is stopped.");
    }
}