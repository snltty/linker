using cmonitor.client.api;
using cmonitor.config;
using cmonitor.plugins.relay.transport;
using common.libs;
using common.libs.api;
using common.libs.extends;
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
                    RelayTransportState state = await relayTransfer.ConnectAsync(param.Content, "test", config.Data.Client.Relay.SecretKey);
                    if (state != null)
                    {
                        var socket = state.Socket;
                        for (int i = 0; i < 10; i++)
                        {
                            Logger.Instance.Debug($"relay [test] send {i}");
                            socket.Send(Encoding.UTF8.GetBytes($"snltty.relay.{i}"));
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
        private void RelayTest()
        {
            relayTransfer.OnConnected += (RelayTransportState state) =>
            {
                if (state.Info.TransactionId == "test")
                {
                    Task.Run(() =>
                    {
                        byte[] bytes = new byte[1024];
                        while (true)
                        {
                            int length = state.Socket.Receive(bytes);
                            if (length == 0) break;

                            Logger.Instance.Debug($"relay [{state.Info.TransactionId}] receive {Encoding.UTF8.GetString(bytes.AsSpan(0,length))}");
                        }
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
