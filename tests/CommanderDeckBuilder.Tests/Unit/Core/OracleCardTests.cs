using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class OracleCardTests
{
    // Test List — Slice 2 (refactor: atomic Oracle facts). Inactive tests are activated one at a time.
    //
    // Suite A — OracleCard value
    //   [x] OracleCardRejectsBlankName
    //   [x] OracleCardRejectsNullTypeLine
    //
    // Suite B — atomic attach (DeckCardOperationsTests / DeckValidationTests)
    //   [x] EntryCreatedWithOracleCardExposesItsFacts
    //   [x] AttachOracleCardReplacesPlaceholderFactsInOneCall
    //   [x] AddingCardWithOracleCardValidatesColorIdentityImmediately
    //   [x] ReaddingPrintingWithOracleCardReplacesAllFacts
    //   verification:
    //   [x] EntryWithoutFactsKeepsPlaceholderDefaults
    //   [x] migrate cardName/cardTypeLine params + setter users to OracleCard; setters removed

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void OracleCardRejectsBlankName(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new OracleCard(name!, "Artifact", ColorIdentity.Parse("C")));
    }

    [Fact]
    public void OracleCardRejectsNullTypeLine()
    {
        Assert.Throws<ArgumentNullException>(
            () => new OracleCard("Sol Ring", null!, ColorIdentity.Parse("C")));
    }

    [Fact]
    public void OracleCardAcceptsEmptyTypeLine()
    {
        Assert.Equal(string.Empty, new OracleCard("Sol Ring", "", ColorIdentity.Parse("C")).TypeLine);
    }
}
