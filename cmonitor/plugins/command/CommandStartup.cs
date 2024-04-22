using cmonitor.config;
using cmonitor.plugins.command.messenger;
using cmonitor.plugins.command.report;
using cmonitor.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace cmonitor.plugins.command
{
    public sealed class CommandStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<CommandReport>();
            serviceCollection.AddSingleton<ICommandLine, CommandLineWindows>();
            serviceCollection.AddSingleton<CommandClientMessenger>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<CommandServerMessenger>();
            serviceCollection.AddSingleton<CommandApiController>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
