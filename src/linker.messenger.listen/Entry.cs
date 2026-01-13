using linker.libs;
using linker.libs.web;
using Microsoft.Extensions.DependencyInjection;
namespace linker.messenger.listen
{
    public static class Entry
    {
        public static ServiceCollection AddListen(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TcpServer>();
            serviceCollection.AddSingleton<IWebApiServer,WebApiServer>();

            serviceCollection.AddSingleton<CountryTransfer>();

            return serviceCollection;
        }
        public static ServiceProvider UseListen(this ServiceProvider serviceProvider)
        {
            CountryTransfer countryTransfer = serviceProvider.GetService<CountryTransfer>();

            TcpServer tcpServer = serviceProvider.GetService<TcpServer>();
            IWebApiServer webapiServer = serviceProvider.GetService<IWebApiServer>();
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

            if (listenStore.ApiPort > 0)
            {
                LoggerHelper.Instance.Info($"start server web api");
                try
                {
                    webapiServer.Start(listenStore.ApiPort);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                LoggerHelper.Instance.Warning($"server web api listen:{listenStore.ApiPort}");
            }

            return serviceProvider;
        }
    }
}
