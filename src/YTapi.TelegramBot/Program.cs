using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using YTapi.Application.Behaviors;
using YTapi.Application.Commands.Handlers;
using YTapi.Application.Interfaces;
using YTapi.Application.Validators;
using YTapi.Infrastructure.BackgroundJobs;
using YTapi.Infrastructure.Configuration;
using YTapi.Infrastructure.ExternalServices.FFmpeg;
using YTapi.Infrastructure.ExternalServices.Interfaces;
using YTapi.Infrastructure.ExternalServices.Spotify;
using YTapi.Infrastructure.ExternalServices.YouTube;
using YTapi.Infrastructure.Proxies;
using YTapi.TelegramBot.Configuration;
using YTapi.TelegramBot.Handlers;
using YTapi.TelegramBot.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/ytapi-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting YTapi Telegram Bot...");

    var builder = Host.CreateApplicationBuilder(args);

    // Add configuration
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>(optional: true);

    // Configure Serilog
    builder.Services.AddSerilog();

    // Configure settings
    builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
    builder.Services.Configure<SpotifySettings>(builder.Configuration.GetSection("Spotify"));
    builder.Services.Configure<YouTubeSettings>(builder.Configuration.GetSection("YouTube"));
    builder.Services.Configure<FFmpegSettings>(builder.Configuration.GetSection("FFmpeg"));
    builder.Services.Configure<ProxySettings>(builder.Configuration.GetSection("Proxy"));
    builder.Services.Configure<DownloadSettings>(builder.Configuration.GetSection("Download"));

    // Register Telegram Bot client
    var telegramToken = builder.Configuration["Telegram:BotToken"]
        ?? throw new InvalidOperationException("Telegram:BotToken is not configured");

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegramToken));

    // Register MediatR
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(DownloadTrackCommandHandler).Assembly);
        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
    });

    // Register FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<DownloadTrackCommandValidator>();

    // Register HTTP Client
    builder.Services.AddHttpClient();

    // Register Infrastructure services
    builder.Services.AddSingleton<ISpotifyClientFactory, SpotifyClientFactory>();
    builder.Services.AddScoped<ISpotifyService, SpotifyService>();
    builder.Services.AddSingleton<IYoutubeClientFactory, YoutubeClientFactory>();
    builder.Services.AddScoped<IYoutubeAudioDownloader, YoutubeAudioDownloader>();
    builder.Services.AddScoped<IAudioConverter, FfmpegAudioConverter>();
    builder.Services.AddSingleton<IProxyProvider, WebshareProxyProvider>();

    // Register Download infrastructure
    builder.Services.AddSingleton<IDownloadQueue, InMemoryDownloadQueue>();
    builder.Services.AddSingleton<IDownloadJobStore, InMemoryDownloadJobStore>();
    builder.Services.AddScoped<IDownloadProcessor, DownloadProcessor>();

    // Register Progress Notifier (Telegram implementation)
    builder.Services.AddSingleton<IProgressNotifier, TelegramProgressNotifier>();

    // Register Telegram handlers
    builder.Services.AddScoped<MessageHandler>();
    builder.Services.AddScoped<CallbackHandler>();

    // Register background services
    builder.Services.AddHostedService<DownloadWorkerService>();
    builder.Services.AddHostedService<TelegramBotService>();

    var host = builder.Build();

    // Initialize Spotify client
    using (var scope = host.Services.CreateScope())
    {
        var spotifyFactory = scope.ServiceProvider.GetRequiredService<ISpotifyClientFactory>();
        Log.Information("Initializing Spotify client...");
        await spotifyFactory.InitializeAsync();
        Log.Information("Spotify client initialized successfully");
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
