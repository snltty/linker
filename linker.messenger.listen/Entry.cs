using linker.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
namespace linker.messenger.listen
{
    public static class Entry
    {
        public static ServiceCollection AddListen(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TcpServer>();
            return serviceCollection;
        }
        public static ServiceProvider UseListen(this ServiceProvider serviceProvider, X509Certificate2 certificate)
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
