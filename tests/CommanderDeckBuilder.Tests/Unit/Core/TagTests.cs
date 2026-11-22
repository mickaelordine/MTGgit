using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class TagTests
{
    // Test List — Slice 1 (refactor: Tag value module). Inactive tests are activated one at a time.
    //
    // Suite A — Tag parse / scope / normalization
    //   [x] ParsesLocalTagWithLowercaseName
    //   [x] ParsesGlobalTagWithCanonicalValue
    //   [x] RemovesWhitespaceAndLowercasesName
    //   [x] RejectsTagWithoutPrefix
    //   [x] RejectsTagWithoutName
    //   verification:
    //   [x] TagsWithSameCanonicalValueAreEqual
    //
    // Suite B — Tag shadow relationship
    //   [ ] GlobalTagShadowsItsLocalTwin
    //   [ ] TagDoesNotShadowOtherScopeOrOtherName

    [Fact]
    public void ParsesLocalTagWithLowercaseName()
    {
        var tag = Tag.Parse("#Ramp");

        Assert.Equal(TagScope.Local, tag.Scope);
        Assert.Equal("ramp", tag.Name);
        Assert.Equal("#ramp", tag.Value);
    }

    [Fact]
    public void ParsesGlobalTagWithCanonicalValue()
    {
        var tag = Tag.Parse("#!Wincon");

        Assert.Equal(TagScope.Global, tag.Scope);
        Assert.Equal("wincon", tag.Name);
        Assert.Equal("#!wincon", tag.Value);
    }

    [Fact]
    public void RemovesWhitespaceAndLowercasesName()
    {
        var tag = Tag.Parse("  #Storm Scale ");

        Assert.Equal("#stormscale", tag.Value);
    }

    [Theory]
    [InlineData("ramp")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void RejectsTagWithoutPrefix(string? raw)
    {
        Assert.ThrowsAny<ArgumentException>(() => Tag.Parse(raw!));
    }

    [Theory]
    [InlineData("#")]
    [InlineData("#!")]
    [InlineData("# ")]
    public void RejectsTagWithoutName(string raw)
    {
        Assert.Throws<ArgumentException>(() => Tag.Parse(raw));
    }

    [Fact]
    public void TagsWithSameCanonicalValueAreEqual()
    {
        Assert.Equal(Tag.Parse("#Ramp"), Tag.Parse("# ramp"));
        Assert.NotEqual(Tag.Parse("#ramp"), Tag.Parse("#!ramp"));
    }
}
