using linker.config;
using linker.messenger.signin;
using linker.plugins.signIn;
using linker.plugins.signIn.args;
using linker.startup;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.signin
{
    public sealed class SignInStartup : IStartup
    {
        public StartupLevel Level => StartupLevel.Normal;
        public string Name => "signin";

        public bool Required => false;

        public string[] Dependent => new string[] { "messenger" };

        public StartupLoadType LoadType => StartupLoadType.Normal;


        public void AddClient(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<SignInArgsTransfer>();

            MemoryPackFormatterProvider.Register(new SignInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInListRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInListResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsResponseItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInResponseInfoFormatter());


            serviceCollection.AddSingleton<PlusSignInClientMessenger>();

           
            serviceCollection.AddSingleton<SignInArgsTypesLoader>();
            serviceCollection.AddSingleton<SignInArgsMachineKeyClient>();
            serviceCollection.AddSingleton<SignInArgsVersionClient>();
            serviceCollection.AddSingleton<SignInClientApiController>();
        }

        public void AddServer(ServiceCollection serviceCollection, FileConfig config)
        {
            serviceCollection.AddSingleton<SignCaching>();
            serviceCollection.AddSingleton<SignInArgsTransfer>();

            MemoryPackFormatterProvider.Register(new SignInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignCacheInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInListRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInListResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsRequestInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsResponseInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInIdsResponseItemInfoFormatter());
            MemoryPackFormatterProvider.Register(new SignInResponseInfoFormatter());

          
            serviceCollection.AddSingleton<ISignInStore, SignInStore>();
            serviceCollection.AddSingleton<PlusSignInServerMessenger>();

          
            serviceCollection.AddSingleton<SignInArgsTypesLoader>();
            serviceCollection.AddSingleton<SignInArgsMachineKeyServer>();
            serviceCollection.AddSingleton<SignInArgsVersionServer>();
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
