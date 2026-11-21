namespace CommanderDeckBuilder.Core;

/// <summary>
/// Provides local caching for Scryfall card artwork.
/// </summary>
public interface IImageCacheService
{
    /// <summary>
    /// Returns the local path for a card image, downloading it when absent.
    /// </summary>
    /// <param name="scryfallId">The Scryfall printing identifier.</param>
    /// <param name="imageUri">The Scryfall CDN image URI.</param>
    /// <param name="cancellationToken">The cancellation token for the download.</param>
    /// <returns>The local cached image path.</returns>
    Task<string> GetImagePathAsync(
        Guid scryfallId,
        Uri imageUri,
        CancellationToken cancellationToken = default);
}
