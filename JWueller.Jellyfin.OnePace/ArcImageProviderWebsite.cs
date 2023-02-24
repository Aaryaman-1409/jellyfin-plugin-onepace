using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace JWueller.Jellyfin.OnePace;

/// <summary>
/// Populates One Pace arc cover art from the project website.
/// </summary>
public class ArcImageProviderWebsite : IRemoteImageProvider, IHasOrder
{
    private readonly OnePaceRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArcImageProviderWebsite> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArcImageProviderWebsite"/> class.
    /// </summary>
    /// <param name="repository">The One Pace repository.</param>
    /// <param name="httpClientFactory">The HTTP client factory used to fetch images.</param>
    /// <param name="logger">The log target for this class.</param>
    public ArcImageProviderWebsite(OnePaceRepository repository, IHttpClientFactory httpClientFactory, ILogger<ArcImageProviderWebsite> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _log = logger;
    }

    /// <summary>
    /// Gets the order of results based on the plugin configuration. A lower order means the result is preferred more.
    /// When we want to prefer website results, we want this to be 0, and 1 otherwise.
    /// </summary>
    public int Order => Convert.ToInt32(Plugin.Instance!.Configuration.PreferDiscordForArcPosters); // 0 or 1

    /// <inheritdoc/>
    public string Name => Plugin.ProviderName;

    /// <inheritdoc/>
    public bool Supports(BaseItem item) => item is Season;

    /// <inheritdoc/>
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new List<ImageType> { ImageType.Primary };

    /// <inheritdoc/>
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var result = new List<RemoteImageInfo>();

        var match = await ArcIdentifier.IdentifyAsync(_repository, ((Season)item).GetLookupInfo(), cancellationToken).ConfigureAwait(false);
        if (match != null)
        {
            foreach (var coverArt in await _repository.FindAllArcCoverArtAsync(match.Number, cancellationToken).ConfigureAwait(false))
            {
                result.Add(new RemoteImageInfo
                {
                    Type = ImageType.Primary,
                    Url = coverArt.Url,
                    Width = coverArt.Width,
                    ProviderName = Name,
                });
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken);
    }
}
