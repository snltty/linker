using cmonitor.client.capi;
using cmonitor.client.tunnel;
using common.libs;
using common.libs.api;
using System.Text;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelApiController : IApiClientController
    {
        private readonly TunnelTransfer tunnelTransfer;

        public TunnelApiController(TunnelTransfer tunnelTransfer)
        {
            this.tunnelTransfer = tunnelTransfer;

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
                            Logger.Instance.Debug($"tunnel {connection.Direction} [test] send {i}");
                            await connection.SendAsync(Encoding.UTF8.GetBytes($"snltty.tunnel.{i}"));
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
            tunnelTransfer.SetConnectedCallback("test", (ITunnelConnection connection) =>
            {
                connection.BeginReceive(async (ITunnelConnection connection, Memory<byte> data, object state) => {

                    Logger.Instance.Debug($"tunnel [{connection.TransactionId}] receive {Encoding.UTF8.GetString(data.Span)}");
                    await Task.CompletedTask;

                }, async (ITunnelConnection connection, object state) => {
                    await Task.CompletedTask;
                }, null);
            });
        }
    }

}
