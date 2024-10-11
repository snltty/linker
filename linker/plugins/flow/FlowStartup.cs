using linker.config;
using linker.plugins.flow.messenger;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.flow
{
    public sealed class FlowStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "flow";
        public bool Required => false;
        public string[] Dependent => new string[] { };
        public StartupLoadType LoadType => StartupLoadType.Dependent;

        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<FlowClientApiController>();
            serviceCollection.AddSingleton<FlowTransfer>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<FlowMessenger>();
            serviceCollection.AddSingleton<FlowTransfer>();
            serviceCollection.AddSingleton<FlowResolver>();
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            FlowTransfer flowTransfer = serviceProvider.GetService<FlowTransfer>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            FlowTransfer flowTransfer = serviceProvider.GetService<FlowTransfer>();
        }
    }
}
