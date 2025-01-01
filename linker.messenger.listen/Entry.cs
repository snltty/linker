using linker.libs;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.listen
{
    public static class Entry
    {
        public static ServiceCollection AddListen(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TcpServer>();
            return serviceCollection;
        }
        public static ServiceProvider UseListen(this ServiceProvider serviceProvider)
        {
            TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
            IListenStore listenStore = serviceProvider.GetService<IListenStore>();

            LoggerHelper.Instance.Info($"start server");
            try
            {
                tcpServer.Start(listenStore.Port);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            LoggerHelper.Instance.Warning($"server listen:{listenStore.Port}");

            return serviceProvider;
        }
    }
}
