using linker.client.config;
using linker.libs;
using linker.plugins.tuntap.config;
using linker.tun;
using System.Net.NetworkInformation;

namespace linker.plugins.tuntap
{
    public sealed class TuntapDeviceStatusTransfer
    {
        private readonly RunningConfig runningConfig;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly LinkerTunDeviceAdapter linkerTunDeviceAdapter;
        private ulong setupTimes = 0;
        public TuntapDeviceStatusTransfer(RunningConfig runningConfig, TuntapTransfer tuntapTransfer, TuntapConfigTransfer tuntapConfigTransfer, LinkerTunDeviceAdapter linkerTunDeviceAdapter)
        {
            this.runningConfig = runningConfig;
            this.tuntapTransfer = tuntapTransfer;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.linkerTunDeviceAdapter = linkerTunDeviceAdapter;

            tuntapTransfer.OnSetupSuccess += () => { setupTimes++; };

            CheckTuntapStatusTask();
        }
        private void CheckTuntapStatusTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (setupTimes > 0 && tuntapConfigTransfer.Running && OperatingSystem.IsWindows())
                {
                    await InterfaceCheck().ConfigureAwait(false);
                    InterfaceOrder();
                }
                return true;
            }, () => 15000);
        }
        private async Task InterfaceCheck()
        {
            if (await InterfaceAvailable() == false && tuntapTransfer.Status != TuntapStatus.Operating)
            {
                LoggerHelper.Instance.Error($"tuntap inerface {tuntapConfigTransfer.DeviceName} is down, restarting");
                linkerTunDeviceAdapter.Shutdown();
                await Task.Delay(5000).ConfigureAwait(false);
                if (await InterfaceAvailable() == false && tuntapTransfer.Status != TuntapStatus.Operating)
                {
                    await tuntapConfigTransfer.RetstartDevice();
                }
            }
        }
        private async Task<bool> InterfaceAvailable()
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == tuntapConfigTransfer.DeviceName);
            return networkInterface != null && networkInterface.OperationalStatus == OperationalStatus.Up && await InterfacePing();
        }
        private async Task<bool> InterfacePing()
        {
            try
            {
                using Ping ping = new Ping();
                PingReply pingReply = await ping.SendPingAsync(tuntapConfigTransfer.IP, 500);
                return pingReply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void InterfaceOrder()
        {
            NetworkInterface linker = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == tuntapConfigTransfer.DeviceName);
            NetworkInterface first = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();

            if (linker != null && linker.Name != first.Name)
            {
                int metricv4 = 0;
                int metricv6 = 0;
                List<string> commands = new List<string> { 
                    $"netsh interface ipv4 set interface \"{tuntapConfigTransfer.DeviceName}\" metric={++metricv4}", 
                    $"netsh interface ipv6 set interface \"{tuntapConfigTransfer.DeviceName}\" metric={++metricv6}"
                };
                commands.AddRange(NetworkInterface.GetAllNetworkInterfaces()
                    .Where(c => c.Name != tuntapConfigTransfer.DeviceName)
                    .Select(c => $"netsh interface ipv4 set interface \"{c.Name}\" metric={++metricv4}"));
                commands.AddRange(NetworkInterface.GetAllNetworkInterfaces()
                    .Where(c => c.Name != tuntapConfigTransfer.DeviceName)
                    .Select(c => $"netsh interface ipv6 set interface \"{c.Name}\" metric={++metricv6}"));
                CommandHelper.Windows(string.Empty, commands.ToArray());
            }
        }
    }
}
