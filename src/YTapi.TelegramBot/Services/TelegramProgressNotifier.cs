using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YTapi.Application.Interfaces;
using YTapi.TelegramBot.Configuration;

namespace YTapi.TelegramBot.Services;

/// <summary>
/// Implements progress notification via Telegram messages.
/// Replaces SignalR for progress updates.
/// </summary>
public sealed class TelegramProgressNotifier : IProgressNotifier
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramProgressNotifier> _logger;

    // Track last update time to avoid rate limiting
    private readonly ConcurrentDictionary<Guid, DateTime> _lastUpdateTime = new();

    public TelegramProgressNotifier(
        ITelegramBotClient botClient,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramProgressNotifier> logger)
    {
        _botClient = botClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ReportProgressAsync(
        Guid jobId,
        long chatId,
        string status,
        double percentage,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableProgressUpdates || chatId == 0)
            return;

        // Rate limit updates
        var now = DateTime.UtcNow;
        if (_lastUpdateTime.TryGetValue(jobId, out var lastUpdate))
        {
            var elapsed = (now - lastUpdate).TotalMilliseconds;
            if (elapsed < _settings.ProgressUpdateIntervalMs && percentage < 100)
                return;
        }

        _lastUpdateTime[jobId] = now;

        try
        {
            var progressBar = GenerateProgressBar(percentage);
            var message = $"""
                *Download Progress*

                {progressBar}
                {percentage:F1}%

                _{status}_
                """;

            // Note: Message editing will be handled by the caller with messageId
            _logger.LogDebug(
                "Progress update for job {JobId}: {Percentage}% - {Status}",
                jobId,
                percentage,
                status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to report progress for job {JobId}", jobId);
        }
    }

    public async Task SendFileAsync(
        Guid jobId,
        long chatId,
        Stream fileStream,
        string fileName,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        if (chatId == 0)
        {
            _logger.LogWarning("Cannot send file: ChatId is 0 for job {JobId}", jobId);
            return;
        }

        try
        {
            fileStream.Position = 0;
            var fileSizeMb = fileStream.Length / (1024.0 * 1024.0);

            _logger.LogInformation(
                "Sending file for job {JobId}: {FileName} ({SizeMb:F2} MB)",
                jobId,
                fileName,
                fileSizeMb);

            if (fileSizeMb > _settings.MaxFileSizeMb)
            {
                await _botClient.SendMessage(
                    chatId,
                    $"File is too large to send via Telegram ({fileSizeMb:F1} MB > {_settings.MaxFileSizeMb} MB limit).\n\nPlease use a smaller file or contact support.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Determine if it's audio or document
            if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            {
                await _botClient.SendAudio(
                    chatId,
                    InputFile.FromStream(fileStream, fileName),
                    caption: caption,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendDocument(
                    chatId,
                    InputFile.FromStream(fileStream, fileName),
                    caption: caption,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }

            _logger.LogInformation(
                "Successfully sent file for job {JobId} to chat {ChatId}",
                jobId,
                chatId);

            // Clean up tracking
            _lastUpdateTime.TryRemove(jobId, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send file for job {JobId}", jobId);
            throw;
        }
    }

    public async Task SendErrorAsync(
        Guid jobId,
        long chatId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (chatId == 0)
            return;

        try
        {
            await _botClient.SendMessage(
                chatId,
                $"*Download Failed*\n\nJob: `{jobId}`\n\nError: {errorMessage}",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            // Clean up tracking
            _lastUpdateTime.TryRemove(jobId, out _);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send error notification for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Updates an existing message with progress (for inline updates).
    /// </summary>
    public async Task UpdateProgressMessageAsync(
        long chatId,
        int messageId,
        string status,
        double percentage,
        string? trackInfo = null,
        CancellationToken cancellationToken = default)
    {
        if (chatId == 0 || messageId == 0)
            return;

        try
        {
            var progressBar = GenerateProgressBar(percentage);
            var message = $"""
                *Downloading...*

                {progressBar} {percentage:F0}%

                {(trackInfo != null ? $"_{trackInfo}_\n\n" : "")}_{status}_
                """;

            await _botClient.EditMessageText(
                chatId,
                messageId,
                message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
        {
            // Ignore - message content is the same
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update progress message");
        }
    }

    private static string GenerateProgressBar(double percentage)
    {
        const int barLength = 10;
        var filled = (int)Math.Round(percentage / 100 * barLength);
        var empty = barLength - filled;

        return $"[{'█'.ToString().PadRight(filled, '█')}{'░'.ToString().PadRight(empty, '░')}]";
    }
}
