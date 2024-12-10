using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.relay.client.transport;
using System.Net;
using System.Net.NetworkInformation;

namespace linker.plugins.relay.client
{
    /// <summary>
    /// 中继
    /// </summary>
    public sealed class RelayTestTransfer
    {
        private readonly FileConfig fileConfig;
        private readonly RelayTransfer relayTransfer;
        private readonly ClientSignInState clientSignInState;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public List<RelayNodeReportInfo> Nodes { get; private set; } = new List<RelayNodeReportInfo>();

        public RelayTestTransfer(FileConfig fileConfig, RelayTransfer relayTransfer, ClientSignInState clientSignInState, ClientConfigTransfer clientConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.relayTransfer = relayTransfer;
            this.clientSignInState = clientSignInState;
            this.clientConfigTransfer = clientConfigTransfer;

            TestTask();
        }


        private readonly LastTicksManager lastTicksManager = new LastTicksManager();
        public void Subscribe()
        {
            lastTicksManager.Update();
        }
        private async Task TaskRelay()
        {
            try
            {
                ITransport transport = relayTransfer.Transports.FirstOrDefault(d => d.Type == fileConfig.Data.Client.Relay.Server.RelayType);
                if (transport != null)
                {
                    Nodes = await transport.RelayTestAsync(new RelayTestInfo
                    {
                        MachineId = clientConfigTransfer.Id,
                        SecretKey = fileConfig.Data.Client.Relay.Server.SecretKey
                    });
                    var tasks = Nodes.Select(async (c) =>
                    {
                        IPEndPoint ep = c.EndPoint == null || c.EndPoint.Address.Equals(IPAddress.Any) ? clientSignInState.Connection.Address : c.EndPoint;

                        using Ping ping = new Ping();
                        var resp = await ping.SendPingAsync(ep.Address, 1000);
                        c.Delay = resp.Status == IPStatus.Success ? (int)resp.RoundtripTime : -1;
                    });
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception)
            {
            }
        }
        private void TestTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (lastTicksManager.DiffLessEqual(3000))
                {
                    await TaskRelay();
                }
                return true;
            }, () => 3000);
        }

    }
}