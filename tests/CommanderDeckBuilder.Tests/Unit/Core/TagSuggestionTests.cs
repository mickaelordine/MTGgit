using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class TagSuggestionTests
{
    [Fact]
    public void CurrentDeckLocalTagsAreSuggested()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);

        var suggestions = new TagSuggestionService().GetSuggestions(deck, [deck], "ramp");

        Assert.Equal(["#ramp"], suggestions);
    }

    [Fact]
    public void GlobalTagsAreSuggestedForEveryDeck()
    {
        var source = CreateDeck();
        source.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#!Wincon"]);
        var target = CreateDeck();

        var suggestions = new TagSuggestionService().GetSuggestions(target, [source], "win");

        Assert.Equal(["#!wincon"], suggestions);
    }

    [Fact]
    public void LocalTagsFromOtherDecksAreExcluded()
    {
        var source = CreateDeck();
        source.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);
        var target = CreateDeck();

        var suggestions = new TagSuggestionService().GetSuggestions(target, [source], "ram");

        Assert.Empty(suggestions);
    }

    [Fact]
    public void SuggestionsAreDeduplicatedAndSorted()
    {
        var deck = CreateDeck();
        deck.AddCard("Card A", "printing-1", DeckZone.Mainboard, ["#Tutor", "#Ramp"]);
        deck.AddCard("Card B", "printing-2", DeckZone.Mainboard, ["#!Ramp", "#Ramp"]);

        var suggestions = new TagSuggestionService().GetSuggestions(deck, [deck], "ramp");

        Assert.Equal(["#!ramp", "#ramp"], suggestions);
    }

    [Fact]
    public void LocalAndGlobalScopesArePreserved()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#!Ramp"]);
        deck.AddTag("printing-1", "#Ramp");

        var suggestions = new TagSuggestionService().GetSuggestions(deck, [deck], "ramp");

        Assert.Equal(["#!ramp"], suggestions);
    }

    [Fact]
    public void PrefixAndSubstringQueriesFilterSuggestions()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Storm Scale", "#Ramp"]);

        var suggestions = new TagSuggestionService().GetSuggestions(deck, [deck], "storm");

        Assert.Equal(["#stormscale"], suggestions);
    }

    [Fact]
    public void EmptyOrWhitespaceQueryReturnsNoSuggestions()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);

        Assert.Empty(new TagSuggestionService().GetSuggestions(deck, [deck], ""));
        Assert.Empty(new TagSuggestionService().GetSuggestions(deck, [deck], "   "));
    }

    [Fact]
    public void UnknownQueryReturnsNoSuggestions()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#Ramp"]);

        Assert.Empty(new TagSuggestionService().GetSuggestions(deck, [deck], "draw"));
    }

    [Fact]
    public void DuplicateDeckSourcesProduceUniqueSuggestions()
    {
        var deck = CreateDeck();
        deck.AddCard("Card", "printing-1", DeckZone.Mainboard, ["#!Ramp"]);

        var suggestions = new TagSuggestionService().GetSuggestions(deck, [deck, deck], "ramp");

        Assert.Equal(["#!ramp"], suggestions);
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
