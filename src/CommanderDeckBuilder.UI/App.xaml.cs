using System.Windows;
using CommanderDeckBuilder.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CommanderDeckBuilder.UI;

/// \brief Defines the WPF application startup composition.
public partial class App : Application
{
    /// \brief Initializes services, creates the database, and displays the main window.
    /// \param e The WPF startup event arguments.
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

