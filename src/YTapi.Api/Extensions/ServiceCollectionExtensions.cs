using FluentValidation;
using MediatR;
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

namespace YTapi.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MediatR
        services.AddMediatR(cfg => 
        {

            cfg.RegisterServicesFromAssembly(typeof(DownloadTrackCommandHandler).Assembly);
            //cfg.RegisterServicesFromAssembly(typeof(DownloadAlbumCommandHandler).Assembly);
            //cfg.RegisterServicesFromAssembly(typeof(GetSpotifyTrackQueryValidator).Assembly);  
            // Add pipeline behaviors (order matters!)
            cfg.AddOpenBehavior(typeof(YTapi.Application.Behaviors.LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(YTapi.Application.Behaviors.ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(YTapi.Application.Behaviors.PerformanceBehavior<,>));
        });

        // FluentValidation - registers all validators in the assembly
        services.AddValidatorsFromAssemblyContaining<DownloadTrackCommandValidator>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<SpotifySettings>(configuration.GetSection("Spotify"));
        services.Configure<YouTubeSettings>(configuration.GetSection("YouTube"));
        services.Configure<FFmpegSettings>(configuration.GetSection("FFmpeg"));
        services.Configure<ProxySettings>(configuration.GetSection("Proxy"));
         services.Configure<DownloadSettings>(
            configuration.GetSection("Download"));

        // HTTP Client
        services.AddHttpClient();

        // Spotify
        services.AddSingleton<ISpotifyClientFactory, SpotifyClientFactory>();
        services.AddScoped<ISpotifyService, SpotifyService>();

        // YouTube
        services.AddSingleton<IYoutubeClientFactory, YoutubeClientFactory>();
        services.AddScoped<IYoutubeAudioDownloader, YoutubeAudioDownloader>();

        // FFmpeg
        services.AddScoped<IAudioConverter, FfmpegAudioConverter>();

        // Proxy
        services.AddSingleton<IProxyProvider, WebshareProxyProvider>();

        // Download infrastructure
        services.AddSingleton<IDownloadQueue, InMemoryDownloadQueue>();
        services.AddSingleton<IDownloadJobStore, InMemoryDownloadJobStore>();
        services.AddScoped<IDownloadProcessor, DownloadProcessor>();

        // Background services
        services.AddHostedService<DownloadWorkerService>();

        return services;
    }

    public static async Task InitializeServicesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var spotifyFactory = scope.ServiceProvider.GetRequiredService<ISpotifyClientFactory>();
        
        app.Logger.LogInformation("Initializing Spotify client...");
        await spotifyFactory.InitializeAsync();
        app.Logger.LogInformation("Spotify client initialized successfully");
    }
}