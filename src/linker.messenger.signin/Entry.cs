using linker.libs;
using linker.libs.api;
using linker.messenger.exroute;
using linker.messenger.signin.args;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.signin
{
    public static class Entry
    {
        public static ServiceCollection AddSignInClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<SignInArgsTransfer>();
            serviceCollection.AddSingleton<SignInArgsGroupPasswordClient>();
            serviceCollection.AddSingleton<SignInArgsSecretKeyClient>();
            serviceCollection.AddSingleton<SignInArgsMachineKeyClient>();
            serviceCollection.AddSingleton<SignInArgsVersionClient>();

            serviceCollection.AddSingleton<SignInClientState>();
            serviceCollection.AddSingleton<SignInClientTransfer>();

            serviceCollection.AddSingleton<SignInApiController>();

            serviceCollection.AddSingleton<SignInExRoute>();

            serviceCollection.AddSingleton<SignInClientMessenger>();

            return serviceCollection;
        }
        public static ServiceProvider UseSignInClient(this ServiceProvider serviceProvider)
        {
            SignInArgsTransfer signInArgsTransfer = serviceProvider.GetService<SignInArgsTransfer>();
            signInArgsTransfer.AddArgs(new List<ISignInArgs> {
                serviceProvider.GetService<SignInArgsGroupPasswordClient>(),
                serviceProvider.GetService<SignInArgsSecretKeyClient>(),
                  serviceProvider.GetService<SignInArgsMachineKeyClient>(),
                serviceProvider.GetService<SignInArgsVersionClient>(),
            });

            linker.messenger.api.IApiServer apiServer = serviceProvider.GetService<linker.messenger.api.IApiServer>();
            apiServer.AddPlugins(new List<IApiController> {
                serviceProvider.GetService<SignInApiController>()
            });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<SignInClientMessenger>() });

            ExRouteTransfer exRouteTransfer= serviceProvider.GetService<ExRouteTransfer>();
            exRouteTransfer.AddExRoutes(new List<IExRoute> { serviceProvider.GetService<SignInExRoute>() });

            LoggerHelper.Instance.Info($"start signin");
            LoggerHelper.Instance.Info($"start signin transfer");
            SignInClientTransfer clientTransfer = serviceProvider.GetService<SignInClientTransfer>();
            clientTransfer.SignInTask();

            return serviceProvider;
        }


        public static ServiceCollection AddSignInServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<SignInArgsTransfer>();
            serviceCollection.AddSingleton<SignInArgsGroupPasswordServer>();
            serviceCollection.AddSingleton<SignInArgsSecretKeyServer>();
            serviceCollection.AddSingleton<SignInArgsMachineKeyServer>();
            serviceCollection.AddSingleton<SignInArgsVersionServer>();

            serviceCollection.AddSingleton<SignInServerMessenger>();
            serviceCollection.AddSingleton<SignInServerCaching>();
            return serviceCollection;
        }
        public static ServiceProvider UseSignInServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<SignInServerMessenger>() });

            SignInServerCaching signInServerCaching = serviceProvider.GetService<SignInServerCaching>();

            SignInArgsTransfer signInArgsTransfer = serviceProvider.GetService<SignInArgsTransfer>();
            signInArgsTransfer.AddArgs(new List<ISignInArgs> {
                serviceProvider.GetService<SignInArgsGroupPasswordServer>(),
                serviceProvider.GetService<SignInArgsSecretKeyServer>(),
                 serviceProvider.GetService<SignInArgsMachineKeyServer>(),
                serviceProvider.GetService<SignInArgsVersionServer>(),
            });

            return serviceProvider;
        }
    }
}
