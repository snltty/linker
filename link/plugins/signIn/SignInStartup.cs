using link.config;
using link.plugins.signin.messenger;
using link.startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace link.plugins.signin
{
    public sealed class SignInStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "signin";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SignInClientMessenger>();
            serviceCollection.AddSingleton<SignInClientApiController>();
        }

        public void AddServer(ServiceCollection serviceCollection, Config config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SignCaching>();
            serviceCollection.AddSingleton<SignInServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, Config config, Assembly[] assemblies)
        {
        }
    }
}
