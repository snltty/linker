using linker.libs.web;
using linker.tunnel;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.mesh
{
    public static class Entry
    {
        public static ServiceCollection AddMeshClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<MeshClientMessenger>();
            serviceCollection.AddSingleton<MeshApiController>();

            serviceCollection.AddSingleton<TransportMesh>();
            serviceCollection.AddSingleton<TunnelWanPortProtocolMesh>();

            serviceCollection.AddSingleton<MeshNodeTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseMeshClient(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<MeshClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<MeshApiController>() });

            TunnelTransfer tunnelTransfer = serviceProvider.GetService<TunnelTransfer>();
            tunnelTransfer.AddTransport(serviceProvider.GetService<TransportMesh>());
            tunnelTransfer.AddProtocol(serviceProvider.GetService<TunnelWanPortProtocolMesh>());

            return serviceProvider;
        }


        public static ServiceCollection AddMeshServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<MeshServerMessenger>();
            return serviceCollection;
        }
        public static ServiceProvider UseMeshServer(this ServiceProvider serviceProvider)
        {
            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<MeshServerMessenger>() });

            return serviceProvider;
        }
    }
}
