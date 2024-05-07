using cmonitor.client;
using cmonitor.client.api;
using cmonitor.client.tunnel;
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

        public void Connect(ApiControllerParamsInfo param)
        {
            Task.Run(async () =>
            {
                try
                {
                    ITunnelConnection connection = await tunnelTransfer.ConnectAsync(param.Content, "test");
                    if (connection != null)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Logger.Instance.Debug($"tunnel [test] send {i}");
                            await connection.SendAsync(BitConverter.GetBytes(i));
                            await Task.Delay(10);
                        }
                        connection.Close();
                    }
                    else
                    {
                        Logger.Instance.Error($"tunnel {param.Content} fail");
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
            tunnelTransfer.SetConnectCallback("test", (ITunnelConnection connection) =>
            {
                connection.BeginReceive(async (ITunnelConnection connection, Memory<byte> data, object state) => {

                    Logger.Instance.Debug($"tunnel [{connection.TransactionId}] receive {BitConverter.ToInt32(data.Span)}");
                    await Task.CompletedTask;

                }, async (ITunnelConnection connection, object state) => {
                    await Task.CompletedTask;
                }, null);
            });
        }
    }

}
