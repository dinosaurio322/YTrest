using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace YTapi.Infrastructure.Hubs; 
/// <summary>
/// SignalR hub for real-time download progress updates.
/// </summary>
public sealed class DownloadHub : Hub
{
    private readonly ILogger<DownloadHub> _logger;

    public DownloadHub(ILogger<DownloadHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a specific download job group to receive updates.
    /// </summary>
    /// <param name="jobId">The download job ID to monitor</param>
    public async Task JoinJobGroup(Guid jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId.ToString());
        _logger.LogInformation(
            "Client {ConnectionId} joined job group {JobId}",
            Context.ConnectionId,
            jobId);
    }

    /// <summary>
    /// Leave a download job group.
    /// </summary>
    /// <param name="jobId">The download job ID to stop monitoring</param>
    public async Task LeaveJobGroup(Guid jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId.ToString());
        _logger.LogInformation(
            "Client {ConnectionId} left job group {JobId}",
            Context.ConnectionId,
            jobId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogWarning(
                exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}