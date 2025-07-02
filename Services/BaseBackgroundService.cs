using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace xhunter74.FilesUploader.Services;

/// <summary>
///     Provides a base implementation for a long-running background service with logging and cancellation support.
/// </summary>
public abstract class BaseBackgroundService : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _stoppingCts = new();
    private Task _executingTask;

    /// <summary>
    /// Gets the logger instance for the background service.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseBackgroundService"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging service events.</param>
    protected BaseBackgroundService(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Logger.LogInformation("BaseBackgroundService initialized.");
    }

    /// <summary>
    /// Disposes the background service and cancels any running tasks.
    /// </summary>
    public virtual void Dispose()
    {
        Logger.LogInformation("Disposing BaseBackgroundService and cancelling tasks.");
        _stoppingCts.Cancel();
    }

    /// <summary>
    /// Starts the background service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the startup completion.</returns>
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting background service: {ServiceName}", GetType().Name);
        _executingTask = ExecuteAsync(_stoppingCts.Token);

        if (_executingTask.IsCompleted)
        {
            Logger.LogWarning("Background service task completed immediately: {ServiceName}", GetType().Name);
            return _executingTask;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the background service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the shutdown completion.</returns>
    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            Logger.LogWarning("StopAsync called but no executing task was found: {ServiceName}", GetType().Name);
            return;
        }

        try
        {
            Logger.LogInformation("Stopping background service: {ServiceName}", GetType().Name);
            await _stoppingCts.CancelAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while stopping background service: {ServiceName}", GetType().Name);
            throw;
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            Logger.LogInformation("Background service stopped: {ServiceName}", GetType().Name);
        }
    }

    /// <summary>
    /// This method is called when the <see cref="IHostedService"/> starts. Implement background processing logic here.
    /// </summary>
    /// <param name="stoppingToken">A cancellation token that is triggered when the service is stopping.</param>
    /// <returns>A task that represents the background execution.</returns>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
}