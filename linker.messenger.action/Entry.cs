using linker.messenger.action;
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
            serviceCollection.AddSingleton<RelayValidatorAction>();
            serviceCollection.AddSingleton<SForwardValidatorAction>();
            return serviceCollection;
        }
        public static ServiceProvider UseActionClient(this ServiceProvider serviceProvider)
        {
            ApiServer apiServer=serviceProvider.GetService<ApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<ActionApiController>() });

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
            return serviceProvider;
        }
    }
}
