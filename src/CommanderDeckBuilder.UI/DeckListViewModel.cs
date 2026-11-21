using System.Collections.ObjectModel;
using CommanderDeckBuilder.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommanderDeckBuilder.UI;

/// <summary>
/// Exposes observable, grouped and filterable deck-list content for the deck view.
/// </summary>
public sealed partial class DeckListViewModel : ObservableObject
{
    /// <summary>
    /// Creates the deck-list view model over a deck.
    /// </summary>
    /// <param name="deck">The deck whose entries are grouped and filtered.</param>
    public DeckListViewModel(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the current grouped deck content.
    /// </summary>
    public ObservableCollection<DeckViewGroup> Groups => throw new NotImplementedException();

    /// <summary>
    /// Gets or sets whether tagged grouping is active.
    /// </summary>
    [ObservableProperty]
    private bool _tagsEnabled;

    /// <summary>
    /// Gets or sets the normalized tag used to filter visible entries.
    /// </summary>
    [ObservableProperty]
    private string? _tagFilter;
}
