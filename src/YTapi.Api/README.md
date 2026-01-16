# 🎵 YTapi - YouTube Audio Downloader API

REST API for downloading audio from YouTube based on Spotify metadata.

## 🚀 Features

- Download tracks from YouTube using Spotify metadata
- Download complete albums as ZIP files
- Real-time progress updates via SignalR
- Background job processing
- Proxy support (Webshare)
- Clean Architecture with CQRS

## 🛠️ Tech Stack

- .NET 8
- MediatR (CQRS)
- FluentValidation
- YoutubeExplode
- SpotifyAPI.Web
- FFMpegCore
- SignalR
- Serilog

## 📦 Setup

### Local Development

1. Clone the repository
```bash
git clone https://github.com/YOUR_USERNAME/ytapi.git
cd ytapi
```

2. Copy `.env.example` to `appsettings.Development.json` and add your credentials

3. Run
```bash
dotnet run --project src/YTapi.Api
```

### Docker
```bash
docker build -t ytapi .
docker run -p 8080:8080 \
  -e SPOTIFY_CLIENT_ID="your_id" \
  -e SPOTIFY_CLIENT_SECRET="your_secret" \
  ytapi
```

## 📚 API Documentation

- Swagger UI: `http://localhost:5177`
- Health Check: `http://localhost:5177/health`

## 📝 License

MIT