using linker.client.config;
using linker.libs;
using linker.plugins.tuntap.config;
using linker.tunnel.connection;
using System.Net.NetworkInformation;
using System.Net;
using linker.config;

namespace linker.plugins.tuntap
{
    public sealed class TuntapPingTransfer
    {
        private readonly TuntapTransfer tuntapTransfer;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly FileConfig config;
        private readonly TuntapProxy tuntapProxy;
        private readonly RunningConfig runningConfig;
        public TuntapPingTransfer(TuntapTransfer tuntapTransfer, TuntapConfigTransfer tuntapConfigTransfer, FileConfig config, TuntapProxy tuntapProxy, RunningConfig runningConfig)
        {
            this.tuntapTransfer = tuntapTransfer;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
            this.runningConfig = runningConfig;
            PingTask();

        }

        private readonly LastTicksManager lastTicksManager = new LastTicksManager();
        public void SubscribePing()
        {
            lastTicksManager.Update();
        }
        private void PingTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (tuntapTransfer.Status == TuntapStatus.Running && lastTicksManager.DiffLessEqual(5000))
                {
                    await Ping();
                }
                return true;
            }, () => tuntapTransfer.Status == TuntapStatus.Running && lastTicksManager.DiffLessEqual(5000) ? 3000 : 30000);
        }
        private async Task Ping()
        {
            if (tuntapTransfer.Status == TuntapStatus.Running && (runningConfig.Data.Tuntap.Switch & TuntapSwitch.ShowDelay) == TuntapSwitch.ShowDelay)
            {
                var items = tuntapConfigTransfer.Infos.Values.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false && (c.Status & TuntapStatus.Running) == TuntapStatus.Running);
                if ((runningConfig.Data.Tuntap.Switch & TuntapSwitch.AutoConnect) != TuntapSwitch.AutoConnect)
                {
                    var connections = tuntapProxy.GetConnections();
                    items = items.Where(c => connections.TryGetValue(c.MachineId, out ITunnelConnection connection) && connection.Connected || c.MachineId == config.Data.Client.Id);
                }

                foreach (var item in items)
                {
                    using Ping ping = new Ping();
                    PingReply pingReply = await ping.SendPingAsync(item.IP, 500);
                    item.Delay = pingReply.Status == IPStatus.Success ? (int)pingReply.RoundtripTime : -1;

                    tuntapConfigTransfer.Version.Add();
                }
            }
        }
    }
}
