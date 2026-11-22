using CommanderDeckBuilder.Core;
using CommanderDeckBuilder.UI;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckListViewModelTests
{
    [Fact]
    public void DefaultGroupsExposeCardCategories()
    {
        var deck = CreateDeck();
        deck.AddCard("Bear", "printing-bear", DeckZone.Mainboard, oracleCard: new OracleCard("Bear", "Creature", ColorIdentity.Parse("C")));
        deck.AddCard("Bolt", "printing-bolt", DeckZone.Mainboard, oracleCard: new OracleCard("Bolt", "Instant", ColorIdentity.Parse("C")));

        var viewModel = new DeckListViewModel(deck);

        Assert.Contains(viewModel.Groups, group => group.Name == "Creature");
        Assert.Contains(viewModel.Groups, group => group.Name == "Instant");
    }

    [Fact]
    public void EnablingTagsRegroupsByTag()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing", DeckZone.Mainboard, ["#Ramp"], oracleCard: new OracleCard("Card", "Artifact", ColorIdentity.Parse("C")));
        var viewModel = new DeckListViewModel(deck);

        viewModel.TagsEnabled = true;

        Assert.Contains(viewModel.Groups, group => group.Name == "#ramp");
    }

    [Fact]
    public void SettingTagFilterLimitsGroupsToMatches()
    {
        var deck = CreateDeck();
        deck.AddCard("Bear", "printing-bear", DeckZone.Mainboard, ["#Ramp"], oracleCard: new OracleCard("Bear", "Creature", ColorIdentity.Parse("C")));
        deck.AddCard("Bolt", "printing-bolt", DeckZone.Mainboard, ["#Draw"], oracleCard: new OracleCard("Bolt", "Instant", ColorIdentity.Parse("C")));
        var viewModel = new DeckListViewModel(deck) { TagsEnabled = true };

        viewModel.TagFilter = "  #  RAMP ";

        var entries = viewModel.Groups.SelectMany(group => group.Entries).ToArray();
        Assert.Single(entries);
        Assert.Equal("printing-bear", entries[0].SelectedPrintingScryfallId);
    }

    [Fact]
    public void TogglingTagsRaisesGroupsCollectionChanged()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing", DeckZone.Mainboard, ["#Ramp"], oracleCard: new OracleCard("Card", "Artifact", ColorIdentity.Parse("C")));
        var viewModel = new DeckListViewModel(deck);
        var raised = false;
        viewModel.Groups.CollectionChanged += (_, _) => raised = true;

        viewModel.TagsEnabled = true;

        Assert.True(raised);
    }

    [Fact]
    public void ClearingTagFilterRestoresGroupedContent()
    {
        var deck = CreateDeck();
        deck.AddCard("Bear", "printing-bear", DeckZone.Mainboard, ["#Ramp"], oracleCard: new OracleCard("Bear", "Creature", ColorIdentity.Parse("C")));
        deck.AddCard("Bolt", "printing-bolt", DeckZone.Mainboard, ["#Draw"], oracleCard: new OracleCard("Bolt", "Instant", ColorIdentity.Parse("C")));
        var viewModel = new DeckListViewModel(deck) { TagsEnabled = true, TagFilter = "#Ramp" };

        viewModel.TagFilter = null;

        var entries = viewModel.Groups.SelectMany(group => group.Entries).ToArray();
        Assert.Equal(2, entries.Length);
    }

    private static Deck CreateDeck()
    {
        return Deck.Create(
            new DeckCommander(
                "Test Commander",
                ColorIdentity.Parse("W,U"),
                DeckCommanderMechanic.Primary));
    }
}
