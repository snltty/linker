using cmonitor.client;
using cmonitor.client.api;
using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.tunnel.server;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
using common.libs.api;
using common.libs.extends;
using MemoryPack;
using System.Net.Sockets;
using static cmonitor.plugins.tunnel.TunnelTransfer;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelApiController : IApiClientController
    {
        private readonly Config config;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientSignInTransfer clientSignInTransfer;
        private readonly MessengerSender messengerSender;
        private readonly TunnelTransfer tunnelTransfer;
        private readonly TunnelBindServer tunnelBindServer;

        public TunnelApiController(Config config, ClientSignInState clientSignInState, ClientSignInTransfer clientSignInTransfer, MessengerSender messengerSender, TunnelTransfer tunnelTransfer,
            TunnelBindServer tunnelBindServer)
        {
            this.config = config;
            this.clientSignInState = clientSignInState;
            this.clientSignInTransfer = clientSignInTransfer;
            this.messengerSender = messengerSender;
            this.tunnelTransfer = tunnelTransfer;
            this.tunnelBindServer = tunnelBindServer;

            TunnelTest();
        }

        public Config Config(ApiControllerParamsInfo param)
        {
            return config;
        }
        public void ConfigSet(ApiControllerParamsInfo param)
        {
            ConfigSetInfo info = param.Content.DeJson<ConfigSetInfo>();
            config.Data.Client.Name = info.Name;
            config.Data.Client.GroupId = info.GroupId;
            config.Data.Client.Server = info.Server;
            config.Save();
            clientSignInTransfer.SignOut();
            _ = clientSignInTransfer.SignIn();
        }
        public void ConfigSetServers(ApiControllerParamsInfo param)
        {
            config.Data.Client.Servers = param.Content.DeJson<ClientServerInfo[]>();
            config.Save();
        }

        public ClientSignInState SignInInfo(ApiControllerParamsInfo param)
        {
            return clientSignInState;
        }
        public async Task SignInDel(ApiControllerParamsInfo param)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.Delete,
                Payload = MemoryPackSerializer.Serialize(param.Content)
            });
        }

        public async Task<SignInListResponseInfo> SignInList(ApiControllerParamsInfo param)
        {
            SignInListRequestInfo request = param.Content.DeJson<SignInListRequestInfo>();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)SignInMessengerIds.List,
                Payload = MemoryPackSerializer.Serialize(request)
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<SignInListResponseInfo>(resp.Data.Span);
            }
            return new SignInListResponseInfo { };
        }
        public Dictionary<string, TunnelConnectInfo> TunnelConnections(ApiControllerParamsInfo param)
        {
            return tunnelTransfer.Connections;
        }
        public void TunnelConnect(ApiControllerParamsInfo param)
        {
            Task.Run(async () =>
            {
                TunnelTransportState state = await tunnelTransfer.ConnectAsync(param.Content,"test");
                if(state != null)
                {
                    var socket = state.ConnectedObject as Socket;
                    for (int i = 0; i < 10; i++)
                    {
                        socket.Send(BitConverter.GetBytes(i));
                        await Task.Delay(10);
                    }
                }
            });
        }
        private void TunnelTest()
        {
            tunnelTransfer.OnConnected += (TunnelTransportState state) =>
            {
                if(state.TransactionId == "test" && state.TransportType == ProtocolType.Tcp)
                {
                    tunnelBindServer.BindReceive(state.ConnectedObject as Socket, null, async (token,data) =>
                    {
                        Console.WriteLine(BitConverter.ToInt32(data.Span));
                    });
                }
            };
        }
    }

    public sealed class ConfigSetInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
        public string Server { get; set; }
    }
}
