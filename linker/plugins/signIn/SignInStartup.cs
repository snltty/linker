using Linker.Config;
using Linker.Plugins.Signin.Messenger;
using Linker.Startup;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Linker.Plugins.Signin
{
    public sealed class SignInStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "signin";

        public bool Required => false;

        public string[] Dependent => new string[] { };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SignInClientMessenger>();
            serviceCollection.AddSingleton<SignInClientApiController>();
        }

        public void AddServer(ServiceCollection serviceCollection, ConfigWrap config, Assembly[] assemblies)
        {
            serviceCollection.AddSingleton<SignCaching>();
            serviceCollection.AddSingleton<SignInServerMessenger>();
        }

        public void UseClient(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
        }

        public void UseServer(ServiceProvider serviceProvider, ConfigWrap config, Assembly[] assemblies)
        {
        }
    }
}
