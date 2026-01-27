using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using YTapi.Application.Commands.Downloads;
using YTapi.Application.Queries.Spotify;

namespace YTapi.TelegramBot.Handlers;

/// <summary>
/// Handles incoming Telegram messages and commands.
/// </summary>
public sealed class MessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IMediator _mediator;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(
        ITelegramBotClient botClient,
        IMediator mediator,
        ILogger<MessageHandler> logger)
    {
        _botClient = botClient;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is null)
            return;

        var chatId = message.Chat.Id;
        var text = message.Text.Trim();

        _logger.LogInformation(
            "Received message from {UserId} in chat {ChatId}: {Text}",
            message.From?.Id,
            chatId,
            text);

        // Handle commands
        if (text.StartsWith('/'))
        {
            await HandleCommandAsync(chatId, text, cancellationToken);
            return;
        }

        // Handle Spotify links
        if (IsSpotifyLink(text))
        {
            await HandleSpotifyLinkAsync(chatId, text, cancellationToken);
            return;
        }

        // Handle search queries
        await HandleSearchAsync(chatId, text, cancellationToken);
    }

    private async Task HandleCommandAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        var parts = text.Split(' ', 2);
        var command = parts[0].ToLowerInvariant().Replace("@", "").Split('@')[0];
        var args = parts.Length > 1 ? parts[1] : string.Empty;

        switch (command)
        {
            case "/start":
                await SendWelcomeMessageAsync(chatId, cancellationToken);
                break;

            case "/help":
                await SendHelpMessageAsync(chatId, cancellationToken);
                break;

            case "/search":
            case "/s":
                if (string.IsNullOrWhiteSpace(args))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Please provide a search query.\nExample: `/search Bohemian Rhapsody`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await HandleSearchAsync(chatId, args, cancellationToken);
                }
                break;

            case "/track":
            case "/t":
                if (string.IsNullOrWhiteSpace(args))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Please provide a Spotify track ID or URL.\nExample: `/track 4cOdK2wGLETKBW3PvgPWqT`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await DownloadTrackAsync(chatId, ExtractSpotifyId(args), cancellationToken);
                }
                break;

            case "/album":
            case "/a":
                if (string.IsNullOrWhiteSpace(args))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Please provide a Spotify album ID or URL.\nExample: `/album 4LH4d3cOWNNsVw41Gqt2kv`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await DownloadAlbumAsync(chatId, ExtractSpotifyId(args), cancellationToken);
                }
                break;

            case "/artist":
                if (string.IsNullOrWhiteSpace(args))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Please provide a Spotify artist ID or URL.\nExample: `/artist 1dfeR4HaWDbWqFHLkxsg1d`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await DownloadArtistAsync(chatId, ExtractSpotifyId(args), cancellationToken);
                }
                break;

            default:
                await _botClient.SendMessage(
                    chatId,
                    "Unknown command. Use /help to see available commands.",
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task SendWelcomeMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        const string welcome = """
            *Welcome to YTapi Bot!*

            I can download music from YouTube based on Spotify metadata.

            *How to use:*
            - Send me a Spotify link (track, album, or artist)
            - Or search for a song by name

            Use /help for more information.
            """;

        await _botClient.SendMessage(
            chatId,
            welcome,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    private async Task SendHelpMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        const string help = """
            *Available Commands:*

            `/search <query>` - Search for tracks
            `/track <id/url>` - Download a track
            `/album <id/url>` - Download an album
            `/artist <id/url>` - Download artist's top tracks

            *Shortcuts:*
            `/s` - Same as /search
            `/t` - Same as /track
            `/a` - Same as /album

            *Tips:*
            - Just paste a Spotify link and I'll handle it
            - Or type a song name to search
            """;

        await _botClient.SendMessage(
            chatId,
            help,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    private async Task HandleSearchAsync(long chatId, string query, CancellationToken cancellationToken)
    {
        var searchingMsg = await _botClient.SendMessage(
            chatId,
            $"Searching for: _{query}_...",
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);

        try
        {
            var searchQuery = new SearchSpotifyTracksQuery(query);
            var result = await _mediator.Send(searchQuery, cancellationToken);

            if (result.IsFailure)
            {
                await _botClient.EditMessageText(
                    chatId,
                    searchingMsg.MessageId,
                    $"Search failed: {result.Error!.Message}",
                    cancellationToken: cancellationToken);
                return;
            }

            var tracks = result.Value!;

            if (!tracks.Any())
            {
                await _botClient.EditMessageText(
                    chatId,
                    searchingMsg.MessageId,
                    "No tracks found for your search.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Create inline keyboard with search results
            var buttons = tracks.Take(5).Select(track =>
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{track.Name} - {string.Join(", ", track.Artists)}",
                        $"dl_track:{track.Id}")
                });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _botClient.EditMessageText(
                chatId,
                searchingMsg.MessageId,
                $"*Search results for:* _{query}_\n\nSelect a track to download:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for tracks");
            await _botClient.EditMessageText(
                chatId,
                searchingMsg.MessageId,
                "An error occurred while searching. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleSpotifyLinkAsync(long chatId, string url, CancellationToken cancellationToken)
    {
        var (type, id) = ParseSpotifyUrl(url);

        if (type is null || id is null)
        {
            await _botClient.SendMessage(
                chatId,
                "Could not parse Spotify URL. Please check the link and try again.",
                cancellationToken: cancellationToken);
            return;
        }

        switch (type)
        {
            case "track":
                await DownloadTrackAsync(chatId, id, cancellationToken);
                break;
            case "album":
                await DownloadAlbumAsync(chatId, id, cancellationToken);
                break;
            case "artist":
                await DownloadArtistAsync(chatId, id, cancellationToken);
                break;
            default:
                await _botClient.SendMessage(
                    chatId,
                    $"Unsupported Spotify type: {type}",
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task DownloadTrackAsync(long chatId, string spotifyId, CancellationToken cancellationToken)
    {
        var statusMsg = await _botClient.SendMessage(
            chatId,
            "Starting track download...",
            cancellationToken: cancellationToken);

        try
        {
            var command = new DownloadTrackCommand(spotifyId, chatId, statusMsg.MessageId);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                await _botClient.EditMessageText(
                    chatId,
                    statusMsg.MessageId,
                    $"Download failed: {result.Error!.Message}",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.EditMessageText(
                    chatId,
                    statusMsg.MessageId,
                    $"Download queued. Job ID: `{result.Value!.JobId}`\n\nPlease wait...",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading track {SpotifyId}", spotifyId);
            await _botClient.EditMessageText(
                chatId,
                statusMsg.MessageId,
                "An error occurred. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task DownloadAlbumAsync(long chatId, string spotifyId, CancellationToken cancellationToken)
    {
        var statusMsg = await _botClient.SendMessage(
            chatId,
            "Starting album download...",
            cancellationToken: cancellationToken);

        try
        {
            var command = new DownloadAlbumCommand(spotifyId, chatId, statusMsg.MessageId);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                await _botClient.EditMessageText(
                    chatId,
                    statusMsg.MessageId,
                    $"Download failed: {result.Error!.Message}",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.EditMessageText(
                    chatId,
                    statusMsg.MessageId,
                    $"Album download queued. Job ID: `{result.Value!.JobId}`\n\nPlease wait...",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading album {SpotifyId}", spotifyId);
            await _botClient.EditMessageText(
                chatId,
                statusMsg.MessageId,
                "An error occurred. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task DownloadArtistAsync(long chatId, string spotifyId, CancellationToken cancellationToken)
    {
        var statusMsg = await _botClient.SendMessage(
            chatId,
            "Starting artist download (top tracks)...",
            cancellationToken: cancellationToken);

        try
        {
            var command = new DownloadArtistCommand(spotifyId, chatId, statusMsg.MessageId);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                await _botClient.EditMessageText(
                    chatId,
                    statusMsg.MessageId,
                    $"Download failed: {result.Error!.Message}",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.EditMessageText(
                    chatId,
                    statusMsg.MessageId,
                    $"Artist download queued. Job ID: `{result.Value!.JobId}`\n\nPlease wait...",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading artist {SpotifyId}", spotifyId);
            await _botClient.EditMessageText(
                chatId,
                statusMsg.MessageId,
                "An error occurred. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    private static bool IsSpotifyLink(string text)
    {
        return text.Contains("open.spotify.com") || text.Contains("spotify:");
    }

    private static string ExtractSpotifyId(string input)
    {
        // If it's a URL, extract the ID
        if (input.Contains("open.spotify.com"))
        {
            var uri = new Uri(input);
            var segments = uri.AbsolutePath.Split('/');
            return segments.LastOrDefault()?.Split('?')[0] ?? input;
        }

        // If it's a URI like spotify:track:xxx
        if (input.StartsWith("spotify:"))
        {
            return input.Split(':').LastOrDefault() ?? input;
        }

        return input;
    }

    private static (string? type, string? id) ParseSpotifyUrl(string url)
    {
        try
        {
            // Handle spotify:type:id format
            if (url.StartsWith("spotify:"))
            {
                var parts = url.Split(':');
                if (parts.Length >= 3)
                    return (parts[1], parts[2]);
            }

            // Handle https://open.spotify.com/type/id format
            if (url.Contains("open.spotify.com"))
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length >= 2)
                {
                    var type = segments[0];
                    var id = segments[1].Split('?')[0];
                    return (type, id);
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return (null, null);
    }
}
