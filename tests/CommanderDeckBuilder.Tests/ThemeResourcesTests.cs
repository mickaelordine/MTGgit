using System.IO;
using System.Xml.Linq;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice3ThemeResourcesTests
{
    [Fact]
    public void App_xaml_merges_the_base_theme_dictionary()
    {
        var app = XDocument.Load(FindRepositoryFile("src/CommanderDeckBuilder.UI/App.xaml"));
        var source = app.Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Attributes()
            .Single(attribute => attribute.Name.LocalName == "Source");

        Assert.Equal("Themes/BaseTheme.xaml", source.Value);
    }

    [Fact]
    public void Base_theme_defines_stable_dynamic_resource_keys()
    {
        var theme = XDocument.Load(FindRepositoryFile("src/CommanderDeckBuilder.UI/Themes/BaseTheme.xaml"));
        var keys = theme.Descendants()
            .Attributes()
            .Where(attribute => attribute.Name.LocalName == "Key")
            .Select(attribute => attribute.Value)
            .ToArray();

        Assert.Contains("WindowBackgroundBrush", keys);
        Assert.Contains("WindowForegroundBrush", keys);
        Assert.Contains("AccentBrush", keys);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".gitignore")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
