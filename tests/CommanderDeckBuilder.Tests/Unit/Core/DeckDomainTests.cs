using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckDomainTests
{
    [Fact]
    public void NewDeckRequiresCommanderSelection()
    {
        Assert.Throws<ArgumentNullException>(() => Deck.Create(null!));
    }

    [Fact]
    public void PrimaryCommanderDerivesDeckColorIdentity()
    {
        var commander = new DeckCommander("Atraxa", ColorIdentity.Parse("W,U,B,G"));

        var deck = Deck.Create(commander);

        Assert.Equal(ColorIdentity.Parse("W,U,B,G"), deck.EffectiveColorIdentity);
    }

    [Fact]
    public void PartnerCommandersCombineColorIdentity()
    {
        var primary = new DeckCommander("Thrasios", ColorIdentity.Parse("U,G"), DeckCommanderMechanic.Partner);
        var partner = new DeckCommander("Akroma", ColorIdentity.Parse("W"), DeckCommanderMechanic.Partner);

        var deck = Deck.Create(primary, partner);

        Assert.Equal(ColorIdentity.Parse("W,U,G"), deck.EffectiveColorIdentity);
    }

    [Fact]
    public void PartnerCommandersCombineColorIdentityWithTwoSameColors()
    {
        var primary = new DeckCommander("Thrasios", ColorIdentity.Parse("U,G"), DeckCommanderMechanic.Partner);
        var partner = new DeckCommander("NotAkroma", ColorIdentity.Parse("G"), DeckCommanderMechanic.Partner);

        var deck = Deck.Create(primary, partner);

        Assert.Equal(ColorIdentity.Parse("U,G"), deck.EffectiveColorIdentity);
    }

    [Fact]
    public void BackgroundAndDoctorCompanionShareCommanderIdentitySlot()
    {
        var background = new DeckCommander("Livaan", ColorIdentity.Parse("R"), DeckCommanderMechanic.Background);
        var companion = new DeckCommander("Pegasus", ColorIdentity.Parse("W"), DeckCommanderMechanic.DoctorsCompanion);

        var backgroundDeck = Deck.Create(new DeckCommander("Esper Sentinel", ColorIdentity.Parse("W"), DeckCommanderMechanic.Primary), background);
        var companionDeck = Deck.Create(new DeckCommander("The Ninth Doctor", ColorIdentity.Parse("W,U"), DeckCommanderMechanic.Primary), companion);

        Assert.Equal(ColorIdentity.Parse("W,R"), backgroundDeck.EffectiveColorIdentity);
        Assert.Equal(ColorIdentity.Parse("W,U"), companionDeck.EffectiveColorIdentity);
    }

    [Fact]
    public void DeckZoneEnumDefinesFourZones()
    {
        Assert.True(Enum.IsDefined(typeof(DeckZone), DeckZone.Commander));
        Assert.True(Enum.IsDefined(typeof(DeckZone), DeckZone.Mainboard));
        Assert.True(Enum.IsDefined(typeof(DeckZone), DeckZone.Sideboard));
        Assert.True(Enum.IsDefined(typeof(DeckZone), DeckZone.Considering));
    }

    [Fact]
    public void DeckCardEntryExposesIssueContract()
    {
        var entry = new DeckCardEntry(
            "oracle-1",
            "printing-1",
            2,
            DeckZone.Mainboard,
            ["#Ramp", "#Tutor"]);

        Assert.Equal("oracle-1", entry.OracleId);
        Assert.Equal("printing-1", entry.SelectedPrintingScryfallId);
        Assert.Equal(2, entry.Quantity);
        Assert.Equal(DeckZone.Mainboard, entry.Zone);
        Assert.Equal(["#ramp", "#tutor"], entry.Tags);
        Assert.False(entry.HasColorIdentityViolation);
    }

    [Fact]
    public void DeckCardCountRequiresExactlyOneHundredCardsInCommanderAndMainboard()
    {
        var entries = new[]
        {
            new DeckCardEntry("commander", "printing-c", 1, DeckZone.Commander),
            new DeckCardEntry("mainboard-1", "printing-1", 98, DeckZone.Mainboard),
            new DeckCardEntry("sideboard", "printing-s", 10, DeckZone.Sideboard),
            new DeckCardEntry("considering", "printing-x", 5, DeckZone.Considering)
        };

        Assert.False(Deck.IsDeckSizeValid(entries));

        entries = [.. entries.Take(2), new DeckCardEntry("mainboard-2", "printing-2", 1, DeckZone.Mainboard)];
        Assert.True(Deck.IsDeckSizeValid(entries));
    }

    [Fact]
    public void ChangingCommanderReDerivesIdentity()
    {
        var deck = Deck.Create(new DeckCommander("Atraxa", ColorIdentity.Parse("W,U,B,G"), DeckCommanderMechanic.Primary));

        deck.ChangePrimaryCommander(new DeckCommander("Kenrith", ColorIdentity.Parse("W,U,B,R,G"), DeckCommanderMechanic.Primary));

        Assert.Equal(ColorIdentity.Parse("W,U,B,R,G"), deck.EffectiveColorIdentity);
    }
}
