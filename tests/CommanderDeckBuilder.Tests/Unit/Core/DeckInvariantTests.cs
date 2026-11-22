using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class DeckInvariantTests
{
    // Test List — Slice 3 (refactor: Deck owns the validity invariant). Inactive tests are activated one at a time.
    //
    // Suite A — pure rule engine (DeckValidationService.Evaluate)
    //   [x] EvaluateReportsViolationsForEveryEntry
    //   verification:
    //   [x] EvaluateKeepsCopyLimitSpecialCases (Rat Colony, Seven Dwarves <= 7, Nazgûl <= 9, basic + snow basic lands)
    //
    // Suite B — Deck owns the invariant
    //   verification:
    //   [x] EveryMutatorLeavesFlagsEqualToEvaluate
    //   [x] TagChangesDoNotAlterViolationFlags
    //   refactor under green: Revalidate() hook; Set*Violation removed; entry mutators internal + InternalsVisibleTo;
    //   redundant Validate calls removed from tests

    [Fact]
    public void EvaluateReportsViolationsForEveryEntry()
    {
        var deck = CreateDeck("U,B");
        deck.AddCard("oracle-bolt", "printing-bolt", DeckZone.Mainboard,
            oracleCard: new OracleCard("Lightning Bolt", "Instant", ColorIdentity.Parse("R")));
        deck.AddCard("oracle-ring", "printing-ring", DeckZone.Mainboard,
            oracleCard: new OracleCard("Sol Ring", "Artifact", ColorIdentity.Parse("C")));
        deck.IncrementCard("printing-ring");

        var results = new DeckValidationService().Evaluate(deck);

        var bolt = Assert.Single(results, result => result.Entry.OracleId == "oracle-bolt");
        Assert.True(bolt.HasColorIdentityViolation);
        Assert.False(bolt.HasCopyLimitViolation);
        var ring = Assert.Single(results, result => result.Entry.OracleId == "oracle-ring");
        Assert.False(ring.HasColorIdentityViolation);
        Assert.True(ring.HasCopyLimitViolation);
        Assert.Equal(2, results.Count);
    }

    [Theory]
    [InlineData("Rat Colony", "Creature — Rat", 30, false)]
    [InlineData("Seven Dwarves", "Creature — Dwarf", 7, false)]
    [InlineData("Seven Dwarves", "Creature — Dwarf", 8, true)]
    [InlineData("Nazgûl", "Creature — Wraith Knight", 9, false)]
    [InlineData("Nazgûl", "Creature — Wraith Knight", 10, true)]
    [InlineData("Mountain", "Basic Land — Mountain", 5, false)]
    [InlineData("Snow-Covered Plains", "Basic Snow Land — Plains", 5, false)]
    [InlineData("Sol Ring", "Artifact", 2, true)]
    public void EvaluateKeepsCopyLimitSpecialCases(
        string name,
        string typeLine,
        int quantity,
        bool expectedViolation)
    {
        var deck = CreateDeck("W,U,B,R,G");
        deck.AddCard("oracle-1", "printing-1", DeckZone.Mainboard,
            oracleCard: new OracleCard(name, typeLine, ColorIdentity.Parse("C")));
        for (var copy = 1; copy < quantity; copy++)
        {
            deck.IncrementCard("printing-1");
        }

        var result = Assert.Single(new DeckValidationService().Evaluate(deck));

        Assert.Equal(expectedViolation, result.HasCopyLimitViolation);
    }

    [Theory]
    [InlineData("AddCard")]
    [InlineData("ReaddCardWithOracleCard")]
    [InlineData("IncrementCard")]
    [InlineData("DecrementCard")]
    [InlineData("RemoveCard")]
    [InlineData("MoveCard")]
    [InlineData("ChangePrinting")]
    [InlineData("ChangePrimaryCommander")]
    [InlineData("ChangeSecondaryCommander")]
    public void EveryMutatorLeavesFlagsEqualToEvaluate(string mutator)
    {
        var deck = CreatePartnerDeckWithViolations();

        Mutate(deck, mutator);

        AssertFlagsMatchEvaluate(deck);
    }

    [Fact]
    public void TagChangesDoNotAlterViolationFlags()
    {
        var deck = CreatePartnerDeckWithViolations();
        var before = deck.Entries
            .Select(entry => (entry.HasColorIdentityViolation, entry.HasCopyLimitViolation))
            .ToArray();

        deck.AddTag("printing-bolt", "#Burn");
        deck.RemoveTag("printing-bolt", "#Burn");

        var after = deck.Entries
            .Select(entry => (entry.HasColorIdentityViolation, entry.HasCopyLimitViolation))
            .ToArray();
        Assert.Equal(before, after);
        AssertFlagsMatchEvaluate(deck);
    }

    private static void Mutate(Deck deck, string mutator)
    {
        var bolt = new OracleCard("Lightning Bolt", "Instant", ColorIdentity.Parse("R"));
        switch (mutator)
        {
            case "AddCard":
                deck.AddCard("oracle-bolt", "printing-bolt-2", DeckZone.Mainboard, oracleCard: bolt);
                break;
            case "ReaddCardWithOracleCard":
                deck.AddCard("oracle-ring", "printing-ring-1", DeckZone.Mainboard,
                    oracleCard: new OracleCard("Sol Ring", "Basic Land — Wastes", ColorIdentity.Parse("C")));
                break;
            case "IncrementCard":
                deck.IncrementCard("printing-bolt");
                break;
            case "DecrementCard":
                deck.DecrementCard("printing-ring-2");
                break;
            case "RemoveCard":
                deck.RemoveCard("printing-bolt");
                break;
            case "MoveCard":
                deck.MoveCard("printing-bolt", DeckZone.Sideboard);
                break;
            case "ChangePrinting":
                deck.ChangePrinting("printing-ring-2", "printing-ring-1");
                break;
            case "ChangePrimaryCommander":
                deck.ChangePrimaryCommander(
                    new DeckCommander("Partner R", ColorIdentity.Parse("R"), DeckCommanderMechanic.Partner));
                break;
            case "ChangeSecondaryCommander":
                deck.ChangeSecondaryCommander(
                    new DeckCommander("Partner R", ColorIdentity.Parse("R"), DeckCommanderMechanic.Partner));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutator), mutator, null);
        }
    }

    private static Deck CreatePartnerDeckWithViolations()
    {
        var deck = Deck.Create(
            new DeckCommander("Partner U", ColorIdentity.Parse("U"), DeckCommanderMechanic.Partner),
            new DeckCommander("Partner B", ColorIdentity.Parse("B"), DeckCommanderMechanic.Partner));
        var ring = new OracleCard("Sol Ring", "Artifact", ColorIdentity.Parse("C"));
        deck.AddCard("oracle-bolt", "printing-bolt", DeckZone.Mainboard,
            oracleCard: new OracleCard("Lightning Bolt", "Instant", ColorIdentity.Parse("R")));
        deck.AddCard("oracle-ring", "printing-ring-1", DeckZone.Mainboard, oracleCard: ring);
        deck.AddCard("oracle-ring", "printing-ring-2", DeckZone.Sideboard, oracleCard: ring);
        return deck;
    }

    private static void AssertFlagsMatchEvaluate(Deck deck)
    {
        var results = new DeckValidationService().Evaluate(deck);
        Assert.Equal(deck.Entries.Count, results.Count);
        Assert.All(results, result =>
        {
            Assert.Equal(result.HasColorIdentityViolation, result.Entry.HasColorIdentityViolation);
            Assert.Equal(result.HasCopyLimitViolation, result.Entry.HasCopyLimitViolation);
        });
    }

    private static Deck CreateDeck(string identity)
    {
        return Deck.Create(
            new DeckCommander("Test Commander", ColorIdentity.Parse(identity), DeckCommanderMechanic.Primary));
    }
}
