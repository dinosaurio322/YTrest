using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YTapi.Application.Interfaces;
using YTapi.Infrastructure.Configuration;

namespace YTapi.Infrastructure.BackgroundJobs;
 
public sealed class DownloadWorkerService : BackgroundService
{
    private readonly IDownloadQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DownloadWorkerService> _logger;
    private readonly DownloadSettings _settings;
    private int _activeWorkers = 0;

    public DownloadWorkerService(
        IDownloadQueue queue,
        IServiceProvider serviceProvider,
        ILogger<DownloadWorkerService> logger,
        IOptions<DownloadSettings> settings)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Download Worker Service is starting");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        _logger.LogInformation(
            "Download Worker Service started. Max concurrent jobs: {MaxConcurrentJobs}, Max concurrent tracks per job: {MaxConcurrentTracks}",
            _settings.MaxConcurrentDownloads,
            _settings.MaxParallelJobs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a job to be available
                var jobId = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Job {JobId} dequeued from queue", jobId);

                // Wait if we're at max capacity
                while (_activeWorkers >= _settings.MaxConcurrentDownloads && !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogDebug(
                        "Max concurrent jobs reached ({MaxConcurrent}). Waiting...",
                        _settings.MaxConcurrentDownloads);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }

                // Process job in background
                _ = ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Download Worker Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Download Worker Service main loop");
                
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Download Worker Service stopped");
    }

    /// <summary>
    /// Processes a single job asynchronously.
    /// </summary>
    private async Task ProcessJobAsync(Guid jobId, CancellationToken stoppingToken)
    {
        Interlocked.Increment(ref _activeWorkers);

        try
        {
            _logger.LogInformation(
                "Starting job {JobId}. Active workers: {ActiveWorkers}/{MaxWorkers}",
                jobId,
                _activeWorkers,
                _settings.MaxConcurrentDownloads);

            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IDownloadProcessor>();

            await processor.ProcessAsync(jobId, stoppingToken);

            _logger.LogInformation(
                "Completed job {JobId}. Active workers: {ActiveWorkers}/{MaxWorkers}",
                jobId,
                _activeWorkers - 1,
                _settings.MaxConcurrentDownloads);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} was cancelled", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
        }
        finally
        {
            Interlocked.Decrement(ref _activeWorkers);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Download Worker Service is stopping. Waiting for active jobs to complete...");

        // Wait for active workers to finish (with timeout)
        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (_activeWorkers > 0 && DateTime.UtcNow - startTime < timeout)
        {
            _logger.LogInformation(
                "Waiting for {ActiveWorkers} active job(s) to complete...",
                _activeWorkers);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        if (_activeWorkers > 0)
        {
            _logger.LogWarning(
                "{ActiveWorkers} job(s) did not complete within timeout",
                _activeWorkers);
        }

        await base.StopAsync(cancellationToken);
    }
}