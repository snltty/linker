using linker.libs.web;
using linker.messenger.action;
using linker.messenger.relay.server.validator;
using linker.messenger.sforward.server.validator;
using linker.messenger.signin.args;
using linker.messenger.sync;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.api
{
    public static class Entry
    {
        public static ServiceCollection AddActionClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ActionApiController>();

            serviceCollection.AddSingleton<ActionTransfer>();
            serviceCollection.AddSingleton<SignInArgsAction>();

            serviceCollection.AddSingleton<ActionClientMessenger>();

            serviceCollection.AddSingleton<ActionSync>();
            return serviceCollection;
        }
        public static ServiceProvider UseActionClient(this ServiceProvider serviceProvider)
        {
            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<ActionApiController>() });

            SignInArgsTransfer signInArgsTransfer = serviceProvider.GetService<SignInArgsTransfer>();
            signInArgsTransfer.AddArgs(new List<ISignInArgsClient> { serviceProvider.GetService<SignInArgsAction>() });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<ActionClientMessenger>() });

            SyncTreansfer syncTransfer = serviceProvider.GetService<SyncTreansfer>();
            syncTransfer.AddSyncs(new List<ISync> { serviceProvider.GetService<ActionSync>() });

            return serviceProvider;
        }

        public static ServiceCollection AddActionServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ActionTransfer>();
            serviceCollection.AddSingleton<SignInArgsAction>();
            serviceCollection.AddSingleton<RelayValidatorAction>();
            serviceCollection.AddSingleton<SForwardValidatorAction>();

            serviceCollection.AddSingleton<ActionServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseActionServer(this ServiceProvider serviceProvider)
        {
            SignInArgsTransfer signInArgsTransfer = serviceProvider.GetService<SignInArgsTransfer>();
            signInArgsTransfer.AddArgs(new List<ISignInArgsClient> { serviceProvider.GetService<SignInArgsAction>() });
            signInArgsTransfer.AddArgs(new List<ISignInArgsServer> { serviceProvider.GetService<SignInArgsAction>() });

            RelayServerValidatorTransfer relayServerValidatorTransfer = serviceProvider.GetService<RelayServerValidatorTransfer>();
            relayServerValidatorTransfer.AddValidators(new List<IRelayServerValidator> { serviceProvider.GetService<RelayValidatorAction>() });

            SForwardValidatorTransfer sForwardValidatorTransfer = serviceProvider.GetService<SForwardValidatorTransfer>();
            sForwardValidatorTransfer.AddValidators(new List<ISForwardValidator> { serviceProvider.GetService<SForwardValidatorAction>() });

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<ActionServerMessenger>() });

            return serviceProvider;
        }
    }
}
