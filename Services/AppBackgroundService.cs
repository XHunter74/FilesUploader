using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using xhunter74.CollectionManager.API.Settings;

namespace xhunter74.FilesUploader.Services;

public class AppBackgroundService : BaseBackgroundService
{
    private readonly SemaphoreSlim _taskSemaphore = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppSettings _appSettings;

    public AppBackgroundService(
        ILogger<AppBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<AppSettings> options
    ) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _appSettings = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation($"{nameof(AppBackgroundService)} is starting.");

        cancellationToken.Register(() =>
            Logger.LogInformation($"{nameof(AppBackgroundService)} task is stopping."));

        while (!cancellationToken.IsCancellationRequested)
        {
            await _taskSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var filesService = scope.ServiceProvider.GetService<FilesService>();
                await filesService.ScanFolderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{GetType().Name}.{nameof(ExecuteAsync)} -> Exception occurred:");
            }
            finally
            {
                _taskSemaphore.Release();
            }

            await Task.Delay(TimeSpan.FromMinutes(_appSettings.ScanIntervalMinutes), cancellationToken);
        }

        Logger.LogInformation($"{nameof(AppBackgroundService)} background task is stopped.");
    }
}