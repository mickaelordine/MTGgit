using Microsoft.Extensions.DependencyInjection;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.UI;

/// \brief Builds the application's dependency-injection service provider.
public static class AppHost
{
    /// \brief Registers application services and creates the service provider.
    /// \return The configured dependency-injection service provider.
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<LocalStoragePaths>();
        services.AddSingleton<SqliteDatabaseInitializer>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }
}
