using cmonitor.client;
using cmonitor.client.running;
using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.viewer.proxy;
using cmonitor.plugins.viewer.report;
using cmonitor.server;
using MemoryPack;

namespace cmonitor.plugins.viewer.messenger
{
    public sealed class ViewerClientMessenger : IMessenger
    {
        private readonly ViewerReport viewerReport;
        private readonly ViewerProxyClient viewerProxyClient;
        private readonly Config config;
        private readonly ClientSignInState clientSignInState;
        private readonly RunningConfig runningConfig;

        public ViewerClientMessenger(ViewerReport viewerReport, ViewerProxyClient viewerProxyClient, Config config, ClientSignInState clientSignInState, RunningConfig runningConfig)
        {
            this.viewerReport = viewerReport;
            this.viewerProxyClient = viewerProxyClient;
            this.config = config;
            this.clientSignInState = clientSignInState;
            this.runningConfig = runningConfig;
        }

        [MessengerId((ushort)ViewerMessengerIds.Server)]
        public void Server(IConnection connection)
        {
            ViewerRunningConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerRunningConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            viewerReport.Server(viewerConfigInfo);
        }

        [MessengerId((ushort)ViewerMessengerIds.Heart)]
        public void Heart(IConnection connection)
        {
            ViewerRunningConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerRunningConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            viewerReport.Heart(viewerConfigInfo);
        }


        [MessengerId((ushort)ViewerMessengerIds.ProxyFromClient)]
        public async Task ProxyFromClient(IConnection connection)
        {
            ViewerProxyInfo proxy = MemoryPackSerializer.Deserialize<ViewerProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            proxy.TargetEP = runningConfig.Data.Viewer.ConnectEP;
            await viewerProxyClient.Connect(proxy);
        }

        [MessengerId((ushort)ViewerMessengerIds.ProxyFromServer)]
        public async Task ProxyFromServer(IConnection connection)
        {
            ViewerProxyInfo proxy = MemoryPackSerializer.Deserialize<ViewerProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            proxy.ProxyEP = new System.Net.IPEndPoint(clientSignInState.Connection.Address.Address,config.Data.Client.Viewer.ProxyPort);
            proxy.TargetEP = runningConfig.Data.Viewer.ConnectEP;
            await viewerProxyClient.Connect(proxy);
        }
    }


    public sealed class ViewerServerMessenger : IMessenger
    {
        private readonly MessengerSender messengerSender;
        private readonly SignCaching signCaching;

        public ViewerServerMessenger(MessengerSender messengerSender, SignCaching signCaching)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
        }



        [MessengerId((ushort)ViewerMessengerIds.HeartNotify)]
        public void HeartNotify(IConnection connection)
        {
            ViewerRunningConfigInfo viewerConfigInfo = MemoryPackSerializer.Deserialize<ViewerRunningConfigInfo>(connection.ReceiveRequestWrap.Payload.Span);
            string[] usernames = viewerConfigInfo.ClientMachines;
            foreach (var item in usernames)
            {
                if (signCaching.Get(item, out SignCacheInfo cache) && cache.Connected)
                {
                    _ = messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)ViewerMessengerIds.Heart,
                        Payload = connection.ReceiveRequestWrap.Payload
                    });
                }
            }
        }

        [MessengerId((ushort)ViewerMessengerIds.ProxyNotify)]
        public void ProxyNotify(IConnection connection)
        {
            ViewerProxyInfo proxy = MemoryPackSerializer.Deserialize<ViewerProxyInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.Get(proxy.ViewerMachine, out SignCacheInfo cache) && cache.Connected)
            {
                _ = messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = cache.Connection,
                    MessengerId = (ushort)ViewerMessengerIds.ProxyFromClient,
                    Payload = connection.ReceiveRequestWrap.Payload
                });
            }
        }
    }
}
