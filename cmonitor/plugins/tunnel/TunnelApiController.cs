using cmonitor.client;
using cmonitor.client.api;
using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.tunnel.server;
using cmonitor.plugins.tunnel.transport;
using cmonitor.server;
using common.libs;
using common.libs.api;
using common.libs.extends;
using MemoryPack;
using System.Net.Sockets;
using static cmonitor.plugins.tunnel.TunnelTransfer;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelApiController : IApiClientController
    {
        private readonly TunnelTransfer tunnelTransfer;
        private readonly TunnelBindServer tunnelBindServer;

        public TunnelApiController(TunnelTransfer tunnelTransfer,
            TunnelBindServer tunnelBindServer)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.tunnelBindServer = tunnelBindServer;

            TunnelTest();
        }

        public Dictionary<string, TunnelConnectInfo> Connections(ApiControllerParamsInfo param)
        {
            return tunnelTransfer.Connections;
        }
        public void Connect(ApiControllerParamsInfo param)
        {
            Task.Run(async () =>
            {
                try
                {
                    TunnelTransportState state = await tunnelTransfer.ConnectAsync(param.Content, "test");
                    if (state != null)
                    {
                        var socket = state.ConnectedObject as Socket;
                        for (int i = 0; i < 10; i++)
                        {
                            Logger.Instance.Debug($"tunnel [test] send {i}");
                            socket.Send(BitConverter.GetBytes(i));
                            await Task.Delay(10);
                        }
                        socket.SafeClose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + "");
                }
            });
        }
        private void TunnelTest()
        {
            tunnelTransfer.OnConnected += (TunnelTransportState state) =>
            {
                if (state.TransactionId == "test" && state.TransportType == ProtocolType.Tcp)
                {
                    tunnelBindServer.BindReceive(state.ConnectedObject as Socket, null, async (token, data) =>
                    {
                        Logger.Instance.Debug($"tunnel [{state.TransactionId}] receive {BitConverter.ToInt32(data.Span)}");
                    });
                }
            };
        }
    }

}
