# 🎵 YTapi - Professional YouTube Audio Download API

> High-performance REST API for downloading audio from YouTube using Spotify metadata with Clean Architecture, background job processing, and real-time updates.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

---

## ✨ Key Features

### 🎧 Audio Downloads
- **Single Track Download** - Download individual songs with high-quality audio (192kbps MP3)
- **Album Download** - Download complete albums as organized ZIP files
- **Top Tracks Download** - Get an artist's top 10 tracks in one request
- **Automatic Metadata** - Fetch track info from Spotify (artist, album, duration)

### ⚡ Performance & Reliability
- **Concurrent Downloads** - Download up to 4 tracks simultaneously per album (configurable)
- **Parallel Job Processing** - Process multiple albums concurrently
- **Smart Retry Logic** - Automatic retry with configurable attempts and delays
- **Download Timeouts** - Per-track timeout to prevent stuck downloads
- **Anti Rate-Limit** - Configurable delays between downloads
- **Background Processing** - Non-blocking downloads with job queue system
- **Proxy Support** - Built-in support for Webshare rotating proxies

### 🔄 Real-time Updates
- **SignalR Integration** - Live download progress updates
- **Job Status Tracking** - Monitor processing, completion, and errors
- **Progress Reporting** - Percentage completion for each download

### 🏗️ Enterprise Architecture
- **Clean Architecture** - Clear separation of concerns
- **CQRS Pattern** - Command Query Responsibility Segregation with MediatR
- **Domain-Driven Design** - Rich domain models and value objects
- **Comprehensive Logging** - Structured logging with Serilog

---

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [FFmpeg](https://ffmpeg.org/download.html) (for audio conversion)
- Spotify API Credentials ([Get them here](https://developer.spotify.com/dashboard))

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/dinosaurio322/YTrest.git
cd YTrest
```

2. **Configure credentials**

Create `appsettings.Development.json` in `src/YTapi.Api/`:

```json
{
  "Spotify": {
    "ClientId": "your_spotify_client_id",
    "ClientSecret": "your_spotify_client_secret"
  },
  "Proxy": {
    "Enabled": false
  }
}
```

3. **Run the API**
```bash
dotnet run --project src/YTapi.Api/YTapi.Api.csproj
```

4. **Open Swagger UI**
```
http://localhost:5177/swagger
```

---

## 🏗️ Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────┐
│  YTapi.Api (Presentation)                           │
│  - Controllers                                      │
│  - Middleware                                       │
│  - SignalR Hubs                                     │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│  YTapi.Application (Business Logic)                 │
│  - CQRS Commands/Queries (MediatR)                 │
│  - DTOs & Mapping                                   │
│  - Validation (FluentValidation)                   │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│  YTapi.Infrastructure (External Services)           │
│  - YouTube Downloader (YoutubeExplode)             │
│  - Spotify Client (SpotifyAPI.Web)                 │
│  - FFmpeg Audio Processing                         │
│  - Background Jobs (Concurrent Processing)         │
│  - SignalR Hubs                                     │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│  YTapi.Domain (Core)                                │
│  - Entities (DownloadJob, SpotifyTrack)            │
│  - Value Objects (Error, Result<T>)                │
│  - Enums (DownloadStatus)                          │
└─────────────────────────────────────────────────────┘
```

### Key Design Patterns

- **CQRS** - Separate read/write operations
- **Mediator** - Decoupled request handling
- **Repository** - Data access abstraction
- **Result Pattern** - Explicit error handling
- **Background Worker** - Async job processing

---

## 📚 API Documentation

### 📥 Downloads Endpoints

#### Download Single Track
```http
POST /api/downloads/track
Content-Type: application/json

{
  "trackId": "3n3Ppam7vgaVa1iaRUc9Lp"  // Spotify Track ID
}

Response: 202 Accepted
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Queued",
  "tracksCount": 1
}
```

#### Download Album
```http
POST /api/downloads/album
Content-Type: application/json

{
  "albumId": "1DFixLWuPkv3KT3TnV35m3"  // Spotify Album ID
}

Response: 202 Accepted
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Queued",
  "tracksCount": 12
}
```

#### Download Artist Top Tracks
```http
POST /api/downloads/artist
Content-Type: application/json

{
  "artistId": "4Z8W4fKeB5YxbusRsdQVPb",  // Spotify Artist ID
  "market": "US"                         // Optional: Country code
}

Response: 202 Accepted
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Queued",
  "tracksCount": 10
}
```

#### Check Download Status
```http
GET /api/downloads/{jobId}

Response: 200 OK
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Processing",
  "progress": 45.5,
  "currentTrack": "Song Title",
  "totalTracks": 12,
  "completedTracks": 5,
  "createdAt": "2025-01-20T10:30:00Z"
}
```

#### Download File
```http
GET /api/downloads/{jobId}/file

Response: 200 OK
Content-Type: application/zip (for albums/artists)
Content-Type: audio/mpeg (for single tracks)
Content-Disposition: attachment; filename="album_name.zip"
```

### 🎵 Spotify Endpoints

#### Search Tracks
```http
GET /api/spotify/search/tracks?q=bohemian+rhapsody&limit=10

Response: 200 OK
{
  "tracks": [
    {
      "id": "3z8h0TU7ReDPLIbEnYhWZb",
      "name": "Bohemian Rhapsody",
      "artist": "Queen",
      "album": "A Night at the Opera",
      "duration": "00:05:55",
      "spotifyUrl": "https://open.spotify.com/track/..."
    }
  ],
  "total": 1000
}
```

#### Get Track Details
```http
GET /api/spotify/tracks/{trackId}

Response: 200 OK
{
  "id": "3z8h0TU7ReDPLIbEnYhWZb",
  "name": "Bohemian Rhapsody",
  "artist": "Queen",
  "album": "A Night at the Opera",
  "trackNumber": 11,
  "duration": "00:05:55",
  "releaseDate": "1975-11-21",
  "spotifyUrl": "https://open.spotify.com/track/...",
  "albumArtUrl": "https://i.scdn.co/image/..."
}
```

#### Get Album Details
```http
GET /api/spotify/albums/{albumId}

Response: 200 OK
{
  "id": "1DFixLWuPkv3KT3TnV35m3",
  "name": "A Night at the Opera",
  "artist": "Queen",
  "releaseDate": "1975-11-21",
  "totalTracks": 12,
  "tracks": [...]
}
```

---

## 🔄 Real-time Updates with SignalR

### Connect to Download Hub

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5177/hubs/download")
    .build();

// Join group for specific job
await connection.start();
await connection.invoke("JoinJobGroup", jobId);

// Listen for progress updates
connection.on("progress", (data) => {
    console.log(`Status: ${data.status}`);
    console.log(`Progress: ${data.percentage}%`);
});
```

### Progress Event Structure

```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Downloading: Track 3/12",
  "percentage": 25.5
}
```

---

## ⚙️ Configuration

### Complete Configuration Reference

```json
{
  "Spotify": {
    "ClientId": "your_client_id",
    "ClientSecret": "your_client_secret",
    "TokenRefreshBufferSeconds": 60
  },
  
  "YouTube": {
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "RetryDelayMilliseconds": 1000,
    "UseExponentialBackoff": true
  },
  
  "FFmpeg": {
    "AudioBitrate": 192,
    "Format": "mp3",
    "Quality": 2,
    "SampleRate": 44100,
    "Channels": 2,
    "NormalizeAudio": false,
    "EmbedAlbumArt": true
  },
  
  "Proxy": {
    "Enabled": true,
    "Provider": "Webshare",
    "Host": "p.webshare.io",
    "Port": 80,
    "Username": "your_username",
    "Password": "your_password",
    "UseHttps": false,
    "RequiresAuthentication": true
  },
  
  "Download": {
    "MaxConcurrentDownloads": 4,
    "MaxParallelJobs": 10,
    "MinDelayBetweenDownloads": 100,
    "DownloadTimeoutSeconds": 300,
    "EnableRetry": true,
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 2000,
    "EnableDetailedProgress": true,
    "BufferSizeMb": 512
  },
  
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Download System Configuration

The download system is fully configurable through `appsettings.json`:

| Setting | Description | Default | Recommended Range |
|---------|-------------|---------|-------------------|
| **MaxConcurrentDownloads** | Tracks downloaded in parallel per album | 4 | 2-8 |
| **MaxParallelJobs** | Albums processed simultaneously | 10 | 5-20 |
| **MinDelayBetweenDownloads** | Delay between downloads (ms) - prevents rate limiting | 100 | 50-200 |
| **DownloadTimeoutSeconds** | Timeout per track (seconds) | 300 | 180-600 |
| **EnableRetry** | Enable automatic retry on failure | true | true |
| **MaxRetryAttempts** | Number of retry attempts per track | 3 | 2-5 |
| **RetryDelayMilliseconds** | Delay between retry attempts (ms) | 2000 | 1000-5000 |
| **EnableDetailedProgress** | Send detailed progress via SignalR | true | true/false |
| **BufferSizeMb** | Buffer size for operations (MB) | 512 | 256-1024 |

### Performance Tuning

All performance settings can be adjusted in `appsettings.json` without code changes:

#### Configuration Profiles

**🐢 Conservative (Small VPS - 2 cores, 4GB RAM)**
```json
{
  "Download": {
    "MaxConcurrentDownloads": 2,
    "MaxParallelJobs": 3,
    "MinDelayBetweenDownloads": 200,
    "DownloadTimeoutSeconds": 600,
    "MaxRetryAttempts": 5
  }
}
```

**⚖️ Balanced (Medium Server - 4 cores, 8GB RAM)** - Default
```json
{
  "Download": {
    "MaxConcurrentDownloads": 4,
    "MaxParallelJobs": 10,
    "MinDelayBetweenDownloads": 100,
    "DownloadTimeoutSeconds": 300,
    "MaxRetryAttempts": 3
  }
}
```

**🚀 Aggressive (Powerful Server - 8+ cores, 16GB RAM)**
```json
{
  "Download": {
    "MaxConcurrentDownloads": 8,
    "MaxParallelJobs": 20,
    "MinDelayBetweenDownloads": 50,
    "DownloadTimeoutSeconds": 180,
    "MaxRetryAttempts": 2
  }
}
```

#### Environment-Specific Configuration

**Development:**
```json
{
  "Download": {
    "MaxConcurrentDownloads": 2,
    "EnableDetailedProgress": true
  }
}
```

**Production:**
```json
{
  "Download": {
    "MaxConcurrentDownloads": 6,
    "MaxParallelJobs": 15,
    "EnableDetailedProgress": false  // Reduce SignalR traffic
  }
}
```

---

## 🐳 Docker Deployment

### Build Image

```bash
docker build -t ytapi:latest .
```

### Run Container

```bash
docker run -d \
  --name ytapi \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Spotify__ClientId="your_client_id" \
  -e Spotify__ClientSecret="your_client_secret" \
  -e Proxy__Enabled=false \
  -e Download__MaxConcurrentDownloads=6 \
  -e Download__MaxParallelJobs=15 \
  ytapi:latest
```

### Docker Compose

```yaml
version: '3.8'

services:
  ytapi:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Spotify__ClientId=${SPOTIFY_CLIENT_ID}
      - Spotify__ClientSecret=${SPOTIFY_CLIENT_SECRET}
      - Proxy__Enabled=false
      - Download__MaxConcurrentDownloads=6
      - Download__MaxParallelJobs=15
      - Download__EnableRetry=true
    volumes:
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

Run:
```bash
docker-compose up -d
```

---

## 🔧 Development

### Project Structure

```
YTrest/
├── src/
│   ├── YTapi.Api/              # REST API & SignalR
│   │   ├── Controllers/        # API endpoints
│   │   ├── Hubs/              # SignalR hubs
│   │   └── Middleware/        # Error handling
│   ├── YTapi.Application/      # Business logic
│   │   ├── Commands/          # Write operations
│   │   ├── Queries/           # Read operations
│   │   ├── DTOs/              # Data transfer objects
│   │   └── Validators/        # FluentValidation
│   ├── YTapi.Infrastructure/   # External services
│   │   ├── BackgroundJobs/    # Job processing
│   │   ├── Services/          # YouTube, Spotify, FFmpeg
│   │   └── Repositories/      # Data access
│   └── YTapi.Domain/           # Core domain
│       ├── Entities/          # Domain models
│       ├── Enums/             # Enumerations
│       └── Common/            # Result pattern, errors
├── tests/
│   ├── YTapi.UnitTests/
│   └── YTapi.IntegrationTests/
└── YTapi.slnx                  # Solution file
```

### Build & Run

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run API
dotnet run --project src/YTapi.Api/YTapi.Api.csproj

# Run with hot reload
dotnet watch --project src/YTapi.Api/YTapi.Api.csproj
```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/YTapi.UnitTests/YTapi.UnitTests.csproj
```

---

## 📊 Performance Metrics

### Download Speed Comparison

**With Default Configuration (MaxConcurrentDownloads: 4)**

| Scenario | Sequential | Concurrent (4x) | Improvement |
|----------|-----------|----------------|-------------|
| 10 track album | ~5 minutes | ~1.25 minutes | **4x faster** |
| 20 track album | ~10 minutes | ~2.5 minutes | **4x faster** |
| 30 track album | ~15 minutes | ~3.75 minutes | **4x faster** |

**With Aggressive Configuration (MaxConcurrentDownloads: 8)**

| Scenario | Sequential | Concurrent (8x) | Improvement |
|----------|-----------|----------------|-------------|
| 30 track album | ~15 minutes | ~2 minutes | **7.5x faster** |
| 50 track album | ~25 minutes | ~3.5 minutes | **7x faster** |

*Average 30 seconds per track download*

### System Requirements & Recommended Settings

| Hardware | CPU | RAM | MaxConcurrentDownloads | MaxParallelJobs |
|----------|-----|-----|----------------------|-----------------|
| Minimum | 2 cores | 4GB | 2 | 3 |
| Recommended | 4 cores | 8GB | 4 | 10 |
| Optimal | 8+ cores | 16GB | 6-8 | 20 |
| Maximum | 16+ cores | 32GB | 12 | 30 |

---

## 🛡️ Health Checks

### Endpoint

```http
GET /health

Response: 200 OK (Healthy) / 503 Service Unavailable (Unhealthy)
{
  "status": "Healthy",
  "checks": {
    "spotify": "Healthy",
    "youtube": "Healthy"
  }
}
```

---

## 🔐 Security Best Practices

1. **Never commit credentials** - Use environment variables or user secrets
2. **Enable HTTPS** in production
3. **Rate limiting** - Consider adding rate limiting middleware
4. **Input validation** - All inputs validated with FluentValidation
5. **Proxy usage** - Use rotating proxies to avoid IP bans

---

## 🐛 Troubleshooting

### Common Issues

#### "Spotify credentials invalid"
```bash
# Solution: Verify credentials in appsettings.json
# Get new credentials: https://developer.spotify.com/dashboard
```

#### "FFmpeg not found"
```bash
# Solution: Install FFmpeg
# Windows: choco install ffmpeg
# Mac: brew install ffmpeg
# Linux: apt-get install ffmpeg
```

#### "Download timeout"
```bash
# Solution: Increase timeout in appsettings.json
"Download": {
  "DownloadTimeoutSeconds": 600  // Increase to 10 minutes
}
```

#### "Downloads are slow"
```bash
# Solution: Increase concurrent downloads
"Download": {
  "MaxConcurrentDownloads": 6,  // Increase from 4
  "MaxParallelJobs": 15          // Increase from 10
}
```

#### "Too many requests / Rate limited"
```bash
# Solution 1: Enable proxy support
"Proxy": {
  "Enabled": true,
  "Host": "p.webshare.io",
  "Port": 80,
  "Username": "your_username",
  "Password": "your_password"
}

# Solution 2: Add delay between downloads
"Download": {
  "MinDelayBetweenDownloads": 500  // Increase to 500ms
}
```

#### "Downloads fail intermittently"
```bash
# Solution: Increase retry attempts
"Download": {
  "EnableRetry": true,
  "MaxRetryAttempts": 5,           // Increase from 3
  "RetryDelayMilliseconds": 3000   // Increase delay
}
```

---

## 🛠️ Tech Stack

### Core Technologies
- **.NET 8** - Latest LTS framework
- **ASP.NET Core** - Web API framework
- **C# 12** - Modern language features

### Libraries & Packages
- **MediatR** - CQRS implementation
- **FluentValidation** - Input validation
- **YoutubeExplode** - YouTube downloader
- **SpotifyAPI.Web** - Spotify integration
- **FFMpegCore** - Audio processing
- **SignalR** - Real-time communication
- **Serilog** - Structured logging

---

## 📝 API Examples

### Complete Workflow Example

```bash
# 1. Search for an album
curl -X GET "http://localhost:5177/api/spotify/search/albums?q=Abbey%20Road&limit=1"

# Response:
# {
#   "albums": [{
#     "id": "0ETFjACtuP2ADo6LFhL6HN",
#     "name": "Abbey Road",
#     "artist": "The Beatles"
#   }]
# }

# 2. Request album download
curl -X POST "http://localhost:5177/api/downloads/album" \
  -H "Content-Type: application/json" \
  -d '{"albumId": "0ETFjACtuP2ADo6LFhL6HN"}'

# Response:
# {
#   "jobId": "550e8400-e29b-41d4-a716-446655440000",
#   "status": "Queued"
# }

# 3. Check status
curl -X GET "http://localhost:5177/api/downloads/550e8400-e29b-41d4-a716-446655440000"

# Response:
# {
#   "status": "Completed",
#   "progress": 100,
#   "completedTracks": 17
# }

# 4. Download file
curl -X GET "http://localhost:5177/api/downloads/550e8400-e29b-41d4-a716-446655440000/file" \
  --output abbey_road.zip
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👤 Author

**Octavio Gomez (CrackerVNTT)**
- Email: octhaviiogomez@gmail.com
- GitHub: [@dinosaurio322](https://github.com/dinosaurio322)

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 🙏 Acknowledgments

- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) - Excellent YouTube downloader library
- [SpotifyAPI.Web](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - Comprehensive Spotify API wrapper
- [FFMpegCore](https://github.com/rosenbjerg/FFMpegCore) - FFmpeg wrapper for .NET

---

## 📮 Support

If you find this project helpful, please consider giving it a ⭐️ on GitHub!

For issues and questions, please open an [issue](https://github.com/dinosaurio322/YTrest/issues).

---

<div align="center">
  
**Built with ❤️ using .NET 8 and Clean Architecture**

</div>