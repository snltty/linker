using cmonitor.config;
using cmonitor.plugins.viewer.messenger;
using cmonitor.server;
using common.libs;
using MemoryPack;

namespace cmonitor.plugins.viewer.proxy
{
    public sealed class ViewerProxyServer : ViewerProxy
    {
        private readonly MessengerSender messengerSender;
        private readonly ViewerProxyCaching viewerProxyCaching;

        public ViewerProxyServer(MessengerSender messengerSender, Config config, ViewerProxyCaching viewerProxyCaching)
        {
            this.messengerSender = messengerSender;
            this.viewerProxyCaching = viewerProxyCaching;

            Start(config.Data.Server.Viewer.ProxyPort);
            Logger.Instance.Info($"start viewer proxy, port : {LocalEndpoint.Port}");
        }

        public override async Task Connect(string name, uint connectId)
        {
            if (viewerProxyCaching.Get(name, out IConnection connection))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = connection,
                    MessengerId = (ushort)ViewerMessengerIds.ProxyFromServer,
                    Payload = MemoryPackSerializer.Serialize(new ViewerProxyInfo
                    {
                        ConnectId = connectId,
                        ProxyEP = string.Empty,
                        ViewerMachine = string.Empty
                    })
                });
            }
        }
    }
}
