using Microsoft.Extensions.DependencyInjection;
using CommanderDeckBuilder.Data;

namespace CommanderDeckBuilder.UI;

public static class AppHost
{
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
