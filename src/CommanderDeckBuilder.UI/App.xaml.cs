using System.Windows;
using CommanderDeckBuilder.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CommanderDeckBuilder.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceProvider = AppHost.CreateServiceProvider();
        serviceProvider.GetRequiredService<SqliteDatabaseInitializer>().Initialize();
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}

