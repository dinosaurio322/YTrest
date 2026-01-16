



using System.Net;
using YoutubeExplode;

namespace YTapi.Infrastructure.ExternalServices.Interfaces;

public interface IYoutubeClientFactory
{
    YoutubeClient Create(WebProxy? proxy = null);
}
