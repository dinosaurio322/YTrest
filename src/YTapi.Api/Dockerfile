

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/YTapi.Api/YTapi.Api.csproj", "YTapi.Api/"]
COPY ["src/YTapi.Application/YTapi.Application.csproj", "YTapi.Application/"]
COPY ["src/YTapi.Infrastructure/YTapi.Infrastructure.csproj", "YTapi.Infrastructure/"]
COPY ["src/YTapi.Domain/YTapi.Domain.csproj", "YTapi.Domain/"]

RUN dotnet restore "YTapi.Api/YTapi.Api.csproj"

# Copy everything else and build
COPY src/ .
RUN dotnet build "YTapi.Api/YTapi.Api.csproj" -c Release -o /app/build
RUN dotnet publish "YTapi.Api/YTapi.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app

# Install FFmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "YTapi.Api.dll"]