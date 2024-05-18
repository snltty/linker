using cmonitor.client.capi;
using cmonitor.client.tunnel;
using cmonitor.config;
using common.libs;
using common.libs.api;
using common.libs.extends;
using System.Text;

namespace cmonitor.plugins.tunnel
{
    public sealed class TunnelApiController : IApiClientController
    {
        private readonly TunnelTransfer tunnelTransfer;
        private readonly Config config;
        public TunnelApiController(TunnelTransfer tunnelTransfer, Config config)
        {
            this.tunnelTransfer = tunnelTransfer;
            this.config = config;
            TunnelTest();
        }

        public bool SetServers(ApiControllerParamsInfo param)
        {
            config.Data.Client.Tunnel.Servers = param.Content.DeJson<TunnelCompactInfo[]>();
            config.Save();
            return true;
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
                        string str = connection.ToString();
                        for (int i = 0; i < 10; i++)
                        {
                            Logger.Instance.Debug($"{str} send {i}");
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
                string str = connection.ToString();
                connection.BeginReceive(async (ITunnelConnection connection, Memory<byte> data, object state) => {

                    Logger.Instance.Debug($"{str} receive {Encoding.UTF8.GetString(data.Span)}");
                    await Task.CompletedTask;

                }, async (ITunnelConnection connection, object state) => {
                    await Task.CompletedTask;
                }, null);
            });
        }
    }

}
