using CommanderDeckBuilder.Core;

namespace CommanderDeckBuilder.Tests;

public sealed class CardNameNormalizerTests
{
    [Theory]
    [InlineData("Nazgûl", "nazgul")]
    [InlineData("NAZGÛL", "nazgul")]
    [InlineData("Æther Vial", "aether vial")]
    [InlineData("  Jötun Grunt ", "jotun grunt")]
    [InlineData("Lim-Dûl the Necromancer", "lim-dul the necromancer")]
    [InlineData("Sol Ring", "sol ring")]
    public void Search_key_ignores_case_and_diacritics(string name, string expected)
    {
        Assert.Equal(expected, CardNameNormalizer.ToSearchKey(name));
    }
}
