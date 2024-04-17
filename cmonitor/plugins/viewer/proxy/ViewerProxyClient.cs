using cmonitor.client;
using cmonitor.client.running;
using cmonitor.config;
using cmonitor.plugins.viewer.messenger;
using cmonitor.server;
using common.libs;
using MemoryPack;

namespace cmonitor.plugins.viewer.proxy
{
    public sealed class ViewerProxyClient : ViewerProxy
    {
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly Config config;
        private readonly RunningConfig runningConfig;

        public ViewerProxyClient(MessengerSender messengerSender, ClientSignInState clientSignInState, Config config, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.runningConfig = runningConfig;

            Start(0);
            Logger.Instance.Info($"start viewer proxy, port : {LocalEndpoint.Port}");
        }

        public override async Task Connect(string name, uint connectId)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ViewerMessengerIds.ProxyNotify,
                Payload = MemoryPackSerializer.Serialize(new ViewerProxyInfo
                {
                    ConnectId = connectId,
                    ProxyEP = $"{clientSignInState.Connection.LocalAddress.Address}:{LocalEndpoint.Port}",
                    ViewerMachine = runningConfig.Data.Viewer.ServerMachine
                })
            });
        }
    }
}
