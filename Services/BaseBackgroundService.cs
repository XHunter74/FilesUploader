using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace xhunter74.FilesUploader.Services;

/// <summary>
///     Base background service
/// </summary>
public abstract class BaseBackgroundService : IHostedService, IDisposable
{
    private readonly CancellationTokenSource _stoppingCts = new();

    private Task _executingTask;

    public ILogger Logger { get; }

    protected BaseBackgroundService(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual void Dispose()
    {
        _stoppingCts.Cancel();
    }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        _executingTask = ExecuteAsync(_stoppingCts.Token);

        if (_executingTask.IsCompleted) return _executingTask;

        return Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null) return;

        try
        {
            await _stoppingCts.CancelAsync();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite,
                cancellationToken));
        }
    }

    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
}