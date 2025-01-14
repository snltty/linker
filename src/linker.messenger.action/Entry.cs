using linker.messenger.action;
using linker.messenger.relay.server.validator;
using linker.messenger.sforward.server.validator;
using linker.messenger.signin.args;
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
            return serviceCollection;
        }
        public static ServiceProvider UseActionClient(this ServiceProvider serviceProvider)
        {
            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<ActionApiController>() });

            SignInArgsTransfer signInArgsTransfer = serviceProvider.GetService<SignInArgsTransfer>();
            signInArgsTransfer.AddArgs(new List<ISignInArgs> { serviceProvider.GetService<SignInArgsAction>() });

            return serviceProvider;
        }

        public static ServiceCollection AddActionServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ActionTransfer>();
            serviceCollection.AddSingleton<SignInArgsAction>();
            serviceCollection.AddSingleton<RelayValidatorAction>();
            serviceCollection.AddSingleton<SForwardValidatorAction>();
            return serviceCollection;
        }
        public static ServiceProvider UseActionServer(this ServiceProvider serviceProvider)
        {
            SignInArgsTransfer signInArgsTransfer = serviceProvider.GetService<SignInArgsTransfer>();
            signInArgsTransfer.AddArgs(new List<ISignInArgs> { serviceProvider.GetService<SignInArgsAction>() });

            RelayServerValidatorTransfer relayServerValidatorTransfer = serviceProvider.GetService<RelayServerValidatorTransfer>();
            relayServerValidatorTransfer.AddValidators(new List<IRelayServerValidator> { serviceProvider.GetService<RelayValidatorAction>() });

            SForwardValidatorTransfer sForwardValidatorTransfer = serviceProvider.GetService<SForwardValidatorTransfer>();
            sForwardValidatorTransfer.AddValidators(new List<ISForwardValidator> { serviceProvider.GetService<SForwardValidatorAction>() });
            return serviceProvider;
        }
    }
}
