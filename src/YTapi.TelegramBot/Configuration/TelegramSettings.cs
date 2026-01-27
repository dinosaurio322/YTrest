namespace YTapi.TelegramBot.Configuration;

/// <summary>
/// Configuration settings for Telegram Bot.
/// </summary>
public sealed class TelegramSettings
{
    /// <summary>
    /// Telegram Bot API Token.
    /// </summary>
    public required string BotToken { get; init; }

    /// <summary>
    /// List of authorized user IDs (optional, empty = allow all).
    /// </summary>
    public List<long> AuthorizedUsers { get; init; } = new();

    /// <summary>
    /// Maximum file size in MB that can be sent via Telegram (default: 50MB).
    /// </summary>
    public int MaxFileSizeMb { get; init; } = 50;

    /// <summary>
    /// Enable progress message updates during downloads.
    /// </summary>
    public bool EnableProgressUpdates { get; init; } = true;

    /// <summary>
    /// Minimum interval between progress updates (milliseconds).
    /// </summary>
    public int ProgressUpdateIntervalMs { get; init; } = 3000;
}
