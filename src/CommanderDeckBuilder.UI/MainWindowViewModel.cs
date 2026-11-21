using CommunityToolkit.Mvvm.ComponentModel;
using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.UI;

/// \brief Provides the MVVM state for the main application window.
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IImageCacheService _imageCacheService;
    private long _artworkRefreshVersion;

    /// <summary>
    /// Creates the main window view model.
    /// </summary>
    /// <param name="imageCacheService">The local artwork cache service.</param>
    public MainWindowViewModel(IImageCacheService imageCacheService)
    {
        _imageCacheService = imageCacheService;
    }

    /// <summary>
    /// Gets the Scryfall identifier of the currently selected printing.
    /// </summary>
    [ObservableProperty]
    private Guid? _selectedPrintingScryfallId;

    /// <summary>
    /// Gets the CDN URI of the currently selected printing.
    /// </summary>
    [ObservableProperty]
    private Uri? _selectedPrintingImageUri;

    /// <summary>
    /// Gets the local cached artwork path for the selected printing.
    /// </summary>
    [ObservableProperty]
    private string? _selectedPrintingImagePath;

    /// <summary>
    /// Refreshes artwork after a deck entry changes its selected printing.
    /// </summary>
    /// <param name="scryfallId">The newly selected printing identifier.</param>
    /// <param name="imageUri">The new Scryfall CDN image URI.</param>
    /// <param name="cancellationToken">The cancellation token for cache lookup.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RefreshArtworkAsync(
        Guid scryfallId,
        Uri imageUri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageUri);
        SelectedPrintingScryfallId = scryfallId;
        SelectedPrintingImageUri = imageUri;
        SelectedPrintingImagePath = null;
        var refreshVersion = ++_artworkRefreshVersion;
        var imagePath = await _imageCacheService.GetImagePathAsync(
            scryfallId,
            imageUri,
            cancellationToken);
        if (refreshVersion == _artworkRefreshVersion)
        {
            SelectedPrintingImagePath = imagePath;
        }
    }
}
