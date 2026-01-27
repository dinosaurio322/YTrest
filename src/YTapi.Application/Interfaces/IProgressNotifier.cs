namespace YTapi.Application.Interfaces;

/// <summary>
/// Interface for notifying download progress to clients.
/// Abstracts the notification mechanism (SignalR, Telegram, etc.)
/// </summary>
public interface IProgressNotifier
{
    /// <summary>
    /// Reports progress for a download job.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="chatId">The chat/user identifier to notify</param>
    /// <param name="status">Current status message</param>
    /// <param name="percentage">Progress percentage (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReportProgressAsync(
        Guid jobId,
        long chatId,
        string status,
        double percentage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a completed file to the user.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="chatId">The chat/user identifier</param>
    /// <param name="fileStream">The file stream to send</param>
    /// <param name="fileName">The file name</param>
    /// <param name="caption">Optional caption for the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendFileAsync(
        Guid jobId,
        long chatId,
        Stream fileStream,
        string fileName,
        string? caption = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an error notification.
    /// </summary>
    /// <param name="jobId">The job identifier</param>
    /// <param name="chatId">The chat/user identifier</param>
    /// <param name="errorMessage">The error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendErrorAsync(
        Guid jobId,
        long chatId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
