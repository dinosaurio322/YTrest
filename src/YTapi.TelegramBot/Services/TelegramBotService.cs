using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YTapi.TelegramBot.Configuration;
using YTapi.TelegramBot.Handlers;

namespace YTapi.TelegramBot.Services;

/// <summary>
/// Background service that runs the Telegram bot.
/// </summary>
public sealed class TelegramBotService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITelegramBotClient botClient,
        IServiceProvider serviceProvider,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation("Telegram Bot started: @{BotUsername} ({BotId})", me.Username, me.Id);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            },
            DropPendingUpdates = true
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Bot is listening for updates...");

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check authorization
            var userId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id ?? 0;

            if (_settings.AuthorizedUsers.Count > 0 && !_settings.AuthorizedUsers.Contains(userId))
            {
                _logger.LogWarning("Unauthorized user attempted access: {UserId}", userId);

                if (update.Message != null)
                {
                    await botClient.SendMessage(
                        update.Message.Chat.Id,
                        "You are not authorized to use this bot.",
                        cancellationToken: cancellationToken);
                }
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<MessageHandler>();
            var callbackHandler = scope.ServiceProvider.GetRequiredService<CallbackHandler>();

            var handler = update switch
            {
                { Message: { } message } => messageHandler.HandleAsync(message, cancellationToken),
                { CallbackQuery: { } callback } => callbackHandler.HandleAsync(callback, cancellationToken),
                _ => Task.CompletedTask
            };

            await handler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update {UpdateId}", update.Id);
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram Bot error occurred");
        return Task.CompletedTask;
    }
}
