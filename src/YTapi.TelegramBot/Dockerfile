# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files
COPY ["src/YTapi.TelegramBot/YTapi.TelegramBot.csproj", "YTapi.TelegramBot/"]
COPY ["src/YTapi.Application/YTapi.Application.csproj", "YTapi.Application/"]
COPY ["src/YTapi.Infrastructure/YTapi.Infrastructure.csproj", "YTapi.Infrastructure/"]
COPY ["src/YTapi.Domain/YTapi.Domain.csproj", "YTapi.Domain/"]

# Restore
RUN dotnet restore "YTapi.TelegramBot/YTapi.TelegramBot.csproj"

# Copy source
COPY src/ .

# Build & Publish
RUN dotnet publish "YTapi.TelegramBot/YTapi.TelegramBot.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview
WORKDIR /app

# Install FFmpeg (necesario para descargas)
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Logs directory
RUN mkdir -p /app/logs

# Environment
ENV DOTNET_ENVIRONMENT=Production

# Run Telegram Bot
ENTRYPOINT ["dotnet", "YTapi.TelegramBot.dll"]
