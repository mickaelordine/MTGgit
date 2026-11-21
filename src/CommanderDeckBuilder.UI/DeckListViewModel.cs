using System.Collections.ObjectModel;
using CommanderDeckBuilder.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommanderDeckBuilder.UI;

/// <summary>
/// Exposes observable, grouped and filterable deck-list content for the deck view.
/// </summary>
public sealed partial class DeckListViewModel : ObservableObject
{
    private readonly Deck _deck;
    private readonly DeckViewProjection _projection = new();

    /// <summary>
    /// Creates the deck-list view model over a deck.
    /// </summary>
    /// <param name="deck">The deck whose entries are grouped and filtered.</param>
    public DeckListViewModel(Deck deck)
    {
        ArgumentNullException.ThrowIfNull(deck);
        _deck = deck;
        Refresh();
    }

    /// <summary>
    /// Gets the current grouped deck content.
    /// </summary>
    public ObservableCollection<DeckViewGroup> Groups { get; } = [];

    private void Refresh()
    {
        Groups.Clear();
        foreach (var group in BuildGroups())
        {
            Groups.Add(group);
        }
    }

    private IEnumerable<DeckViewGroup> BuildGroups()
    {
        var groups = _projection.GetGroups(_deck, TagsEnabled);
        if (string.IsNullOrWhiteSpace(TagFilter))
        {
            return groups;
        }

        var matches = _projection.FilterByTag(_deck, TagFilter).ToHashSet();
        return groups
            .Select(group => group with { Entries = group.Entries.Where(matches.Contains).ToArray() })
            .Where(group => group.Entries.Count > 0);
    }

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

    partial void OnTagsEnabledChanged(bool value) => Refresh();

    partial void OnTagFilterChanged(string? value) => Refresh();
}
