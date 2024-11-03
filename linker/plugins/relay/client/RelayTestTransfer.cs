using linker.config;
using linker.libs;
using linker.plugins.relay.client.transport;
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

        public List<RelayNodeReportInfo> Nodes { get; private set; } = new List<RelayNodeReportInfo>();

        public RelayTestTransfer(FileConfig fileConfig, RelayTransfer relayTransfer)
        {
            this.fileConfig = fileConfig;
            this.relayTransfer = relayTransfer;
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
                        MachineId = fileConfig.Data.Client.Id,
                        SecretKey = fileConfig.Data.Client.Relay.Server.SecretKey
                    });
                    var tasks = Nodes.Select(async (c) =>
                    {
                        using Ping ping = new Ping();
                        var resp = await ping.SendPingAsync(c.EndPoint.Address, 1000);
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
            }, () => lastTicksManager.DiffLessEqual(3000) ? 3000 : 30000);
        }

    }
}