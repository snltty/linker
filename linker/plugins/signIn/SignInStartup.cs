using linker.config;
using linker.plugins.signin.messenger;
using linker.plugins.signIn;
using linker.plugins.signIn.args;
using linker.startup;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.signin
{
    public sealed class SignInStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "signin";

        public bool Required => false;

        public string[] Dependent => new string[] { "messenger", };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<SignInClientMessenger>();
            serviceCollection.AddSingleton<SignInClientApiController>();

            serviceCollection.AddSingleton<SignInArgsTransfer>();
            serviceCollection.AddSingleton<SignInArgsTypesLoader>();
            
            serviceCollection.AddSingleton<SignInArgsMachineKeyClient>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<SignCaching>();
            serviceCollection.AddSingleton<SignInServerMessenger>();

            serviceCollection.AddSingleton<SignInArgsTransfer>();
            serviceCollection.AddSingleton<SignInArgsTypesLoader>();
            serviceCollection.AddSingleton<SignInArgsMachineKeyServer>();

            serviceCollection.AddSingleton<SignInConfigTransfer>();

            
        }

        public void UseClient(ServiceProvider serviceProvider, FileConfig config)
        {
            SignInArgsTypesLoader signInArgsTypesLoader = serviceProvider.GetService<SignInArgsTypesLoader>();
        }

        public void UseServer(ServiceProvider serviceProvider, FileConfig config)
        {
            SignInArgsTypesLoader signInArgsTypesLoader = serviceProvider.GetService<SignInArgsTypesLoader>();
        }
    }
}
