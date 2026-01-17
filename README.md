# 🎵 YTapi - YouTube Audio Downloader API

REST API for downloading audio from YouTube based on Spotify metadata with Clean Architecture.

## 🚀 Features

- ✅ Download single tracks from YouTube using Spotify metadata
- ✅ Download complete albums as ZIP files
- ✅ Download artist's top 10 tracks
- ✅ Real-time progress updates via SignalR
- ✅ Background job processing with parallel downloads
- ✅ Proxy support (Webshare rotating proxies)
- ✅ Clean Architecture with CQRS pattern
- ✅ Comprehensive logging with Serilog
- ✅ Health checks for monitoring
- ✅ Swagger/OpenAPI documentation

## 🏗️ Architecture
```
YTapi.Api           (Presentation Layer)
    ↓
YTapi.Application   (Business Logic - CQRS/MediatR)
    ↓
YTapi.Infrastructure (External Services)
    ↓
YTapi.Domain        (Core Entities & Value Objects)
```

## 🛠️ Tech Stack

- .NET 8
- MediatR (CQRS)
- FluentValidation
- YoutubeExplode
- SpotifyAPI.Web
- FFMpegCore
- SignalR (Real-time updates)
- Serilog (Logging)

## 📦 Environment Variables

Required environment variables for deployment:
```bash
# Spotify API (Required)
Spotify__ClientId=your_spotify_client_id
Spotify__ClientSecret=your_spotify_client_secret

# Proxy (Optional)
Proxy__Enabled=false
Proxy__Host=p.webshare.io
Proxy__Port=80
Proxy__Username=your_username
Proxy__Password=your_password

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

## 🚀 Quick Start

### Local Development

1. Clone the repository
```bash
git clone https://github.com/dinosaurio322/YTrest.git
cd ytapi
```

2. Create `appsettings.Development.json` with your credentials
```bash
cp .env.example appsettings.Development.json
# Edit with your actual credentials
```

3. Run
```bash
dotnet run --project src/YTapi.Api
```

4. Open Swagger UI
```
http://localhost:5177
```

### Docker
```bash
docker build -t ytapi .
docker run -p 8080:8080 \
  -e Spotify__ClientId="your_id" \
  -e Spotify__ClientSecret="your_secret" \
  ytapi
```

## 📚 API Documentation

Once running, visit:
- **Swagger UI**: `http://localhost:5177`
- **Health Check**: `http://localhost:5177/health`
- **SignalR Hub**: `ws://localhost:5177/hubs/download`

## 🎯 Endpoints

### Downloads
- `POST /api/downloads/track` - Download single track
- `POST /api/downloads/album` - Download album (ZIP)
- `POST /api/downloads/artist` - Download artist's top tracks (ZIP)
- `GET /api/downloads/{id}` - Get download status
- `GET /api/downloads/{id}/file` - Download file

### Spotify
- `GET /api/spotify/tracks/{id}` - Get track info
- `GET /api/spotify/albums/{id}` - Get album info
- `GET /api/spotify/artists/{id}` - Get artist info
- `GET /api/spotify/artists/{id}/top-tracks` - Get top tracks
- `GET /api/spotify/search/tracks?q={query}` - Search tracks
- `GET /api/spotify/search/albums?q={query}` - Search albums
- `GET /api/spotify/search/artists?q={query}` - Search artists

## 🔧 Configuration

### Parallel Downloads

Configure concurrent downloads in `appsettings.json`:
```json
{
  "Download": {
    "MaxConcurrentJobs": 10,
    "MaxConcurrentDownloadsPerJob": 3
  }
}
```

### Proxy Configuration

Enable proxy for YouTube downloads:
```json
{
  "Proxy": {
    "Enabled": true,
    "Host": "p.webshare.io",
    "Port": 80,
    "Username": "your_username",
    "Password": "your_password"
  }
}
```

## 📝 License

MIT

## 👤 Author

**CrackerVNTT**
- Email: octhaviiogomez@gmail.com

## 🤝 Contributing

Contributions, issues and feature requests are welcome!