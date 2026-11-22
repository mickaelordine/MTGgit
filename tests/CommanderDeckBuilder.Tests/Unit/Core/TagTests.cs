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
    //   [x] RejectsBlankTag (split from RejectsTagWithoutPrefix at suite-close review)
    //   [x] RejectsTagWithoutName
    //   verification:
    //   [x] TagsWithSameCanonicalValueAreEqual
    //
    // Suite B — Tag shadow relationship
    //   [x] GlobalTagShadowsItsLocalTwin
    //   [x] TagDoesNotShadowOtherScopeOrOtherName

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

    [Theory]
    [InlineData("  #Storm Scale ")]
    [InlineData("  # Storm Scale ")]
    public void RemovesWhitespaceAndLowercasesName(string raw)
    {
        var tag = Tag.Parse(raw);

        Assert.Equal("#stormscale", tag.Value);
    }

    [Fact]
    public void RejectsTagWithoutPrefix()
    {
        Assert.Throws<ArgumentException>(() => Tag.Parse("ramp"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void RejectsBlankTag(string? raw)
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

    [Fact]
    public void GlobalTagShadowsItsLocalTwin()
    {
        Assert.True(Tag.Parse("#!ramp").Shadows(Tag.Parse("#ramp")));
    }

    [Theory]
    [InlineData("#ramp", "#!ramp")]
    [InlineData("#!ramp", "#draw")]
    [InlineData("#!ramp", "#!ramp")]
    [InlineData("#ramp", "#ramp")]
    public void TagDoesNotShadowOtherScopeOrOtherName(string tag, string other)
    {
        Assert.False(Tag.Parse(tag).Shadows(Tag.Parse(other)));
    }
}
