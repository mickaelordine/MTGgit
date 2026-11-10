using System.IO;
using System.Threading;
using System.Xml.Linq;
using CommanderDeckBuilder.UI;
using Microsoft.Extensions.DependencyInjection;

namespace CommanderDeckBuilder.Tests;

public sealed class Slice1WpfStartupTests
{
    [Fact]
    public void Solution_contains_the_four_requested_projects()
    {
        Assert.True(File.Exists(FindRepositoryFile("CommanderDeckBuilder.sln")));
        Assert.True(File.Exists(FindRepositoryFile("src/CommanderDeckBuilder.Core/CommanderDeckBuilder.Core.csproj")));
        Assert.True(File.Exists(FindRepositoryFile("src/CommanderDeckBuilder.Data/CommanderDeckBuilder.Data.csproj")));
        Assert.True(File.Exists(FindRepositoryFile("src/CommanderDeckBuilder.UI/CommanderDeckBuilder.UI.csproj")));
        Assert.True(File.Exists(FindRepositoryFile("tests/CommanderDeckBuilder.Tests/CommanderDeckBuilder.Tests.csproj")));
    }

    [Fact]
    public void Ui_project_targets_net8_windows_with_wpf_enabled()
    {
        var project = XDocument.Load(FindRepositoryFile("src/CommanderDeckBuilder.UI/CommanderDeckBuilder.UI.csproj"));
        var properties = project.Descendants("PropertyGroup").SelectMany(group => group.Elements())
            .ToDictionary(element => element.Name.LocalName, element => element.Value);

        Assert.Equal("net8.0-windows", properties["TargetFramework"]);
        Assert.Equal("true", properties["UseWPF"]);
    }

    [Fact]
    public void Service_provider_registers_the_main_window_and_view_model()
    {
        RunOnSta(() =>
        {
            using var provider = AppHost.CreateServiceProvider();

            Assert.NotNull(provider.GetService<MainWindow>());
            Assert.NotNull(provider.GetService<MainWindowViewModel>());
        });
    }

    [Fact]
    public void Main_window_resolves_with_the_view_model_as_data_context()
    {
        RunOnSta(() =>
        {
            using var provider = AppHost.CreateServiceProvider();

            var window = provider.GetRequiredService<MainWindow>();

            Assert.IsType<MainWindowViewModel>(window.DataContext);
        });
    }

    [Fact]
    public void App_xaml_does_not_define_a_hardcoded_startup_uri()
    {
        var app = XDocument.Load(FindRepositoryFile("src/CommanderDeckBuilder.UI/App.xaml"));

        Assert.DoesNotContain(app.Root!.Attributes(), attribute => attribute.Name.LocalName == "StartupUri");
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

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            throw new AggregateException(failure);
        }
    }
}
