using MediatR;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YTapi.Application.Commands.Downloads;

namespace YTapi.TelegramBot.Handlers;

/// <summary>
/// Handles Telegram callback queries from inline keyboards.
/// </summary>
public sealed class CallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IMediator _mediator;
    private readonly ILogger<CallbackHandler> _logger;

    public CallbackHandler(
        ITelegramBotClient botClient,
        IMediator mediator,
        ILogger<CallbackHandler> logger)
    {
        _botClient = botClient;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(CallbackQuery callback, CancellationToken cancellationToken)
    {
        if (callback.Data is null || callback.Message is null)
            return;

        var chatId = callback.Message.Chat.Id;
        var messageId = callback.Message.MessageId;
        var data = callback.Data;

        _logger.LogInformation(
            "Received callback from {UserId}: {Data}",
            callback.From.Id,
            data);

        // Answer callback to remove loading state
        await _botClient.AnswerCallbackQuery(
            callback.Id,
            cancellationToken: cancellationToken);

        try
        {
            if (data.StartsWith("dl_track:"))
            {
                var trackId = data.Replace("dl_track:", "");
                await DownloadTrackAsync(chatId, messageId, trackId, cancellationToken);
            }
            else if (data.StartsWith("dl_album:"))
            {
                var albumId = data.Replace("dl_album:", "");
                await DownloadAlbumAsync(chatId, messageId, albumId, cancellationToken);
            }
            else if (data.StartsWith("dl_artist:"))
            {
                var artistId = data.Replace("dl_artist:", "");
                await DownloadArtistAsync(chatId, messageId, artistId, cancellationToken);
            }
            else if (data == "cancel")
            {
                await _botClient.EditMessageText(
                    chatId,
                    messageId,
                    "Operation cancelled.",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling callback: {Data}", data);
            await _botClient.EditMessageText(
                chatId,
                messageId,
                "An error occurred. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task DownloadTrackAsync(
        long chatId,
        int messageId,
        string trackId,
        CancellationToken cancellationToken)
    {
        await _botClient.EditMessageText(
            chatId,
            messageId,
            "Starting track download...",
            cancellationToken: cancellationToken);

        var command = new DownloadTrackCommand(trackId, chatId, messageId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                $"Download failed: {result.Error!.Message}",
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                $"Download queued. Job ID: `{result.Value!.JobId}`\n\nPlease wait...",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }

    private async Task DownloadAlbumAsync(
        long chatId,
        int messageId,
        string albumId,
        CancellationToken cancellationToken)
    {
        await _botClient.EditMessageText(
            chatId,
            messageId,
            "Starting album download...",
            cancellationToken: cancellationToken);

        var command = new DownloadAlbumCommand(albumId, chatId, messageId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                $"Download failed: {result.Error!.Message}",
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                $"Album download queued. Job ID: `{result.Value!.JobId}`\n\nPlease wait...",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }

    private async Task DownloadArtistAsync(
        long chatId,
        int messageId,
        string artistId,
        CancellationToken cancellationToken)
    {
        await _botClient.EditMessageText(
            chatId,
            messageId,
            "Starting artist download (top tracks)...",
            cancellationToken: cancellationToken);

        var command = new DownloadArtistCommand(artistId, chatId, messageId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                $"Download failed: {result.Error!.Message}",
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.EditMessageText(
                chatId,
                messageId,
                $"Artist download queued. Job ID: `{result.Value!.JobId}`\n\nPlease wait...",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }
}
