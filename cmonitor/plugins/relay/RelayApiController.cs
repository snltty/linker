using cmonitor.client.api;
using cmonitor.client.tunnel;
using cmonitor.config;
using common.libs;
using common.libs.api;
using System.Text;

namespace cmonitor.plugins.relay
{
    public sealed class RelayApiController : IApiClientController
    {
        private readonly Config config;
        private readonly RelayTransfer relayTransfer;

        public RelayApiController(Config config, RelayTransfer relayTransfer)
        {
            this.config = config;
            this.relayTransfer = relayTransfer;

            RelayTest();
        }

        public void Connect(ApiControllerParamsInfo param)
        {
            Task.Run(async () =>
            {
                try
                {
                    ITunnelConnection connection = await relayTransfer.ConnectAsync(param.Content, "test", config.Data.Client.Relay.SecretKey);
                    if (connection != null)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Logger.Instance.Debug($"relay [test] send {i}");
                            await connection.SendAsync(Encoding.UTF8.GetBytes($"snltty.relay.{i}"));
                            await Task.Delay(10);
                        }
                        connection.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + "");
                }
            });
        }
        private void RelayTest()
        {
            relayTransfer.SetConnectCallback("test", (ITunnelConnection connection) =>
            {
                Task.Run(() =>
                {
                    connection.BeginReceive(async (ITunnelConnection connection, Memory<byte> data, object state) =>
                    {
                        Logger.Instance.Debug($"relay [{connection.TransactionId}] receive {Encoding.UTF8.GetString(data.Span)}");
                        await Task.CompletedTask;
                    },
                    async (ITunnelConnection connection, object state) =>
                    {
                        await Task.CompletedTask;
                    }, null);
                });
            });
        }
    }

    public sealed class ConfigSetInfo
    {
        public string Name { get; set; }
        public string GroupId { get; set; }
        public string Server { get; set; }
    }
}
