using linker.client.config;
using linker.libs;
using linker.plugins.tuntap.config;
using linker.tun;
using System.Net.NetworkInformation;

namespace linker.plugins.tuntap.client
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
        }
        private void CheckTuntapStatusTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (setupTimes > 0 && runningConfig.Data.Tuntap.Running && OperatingSystem.IsWindows())
                {
                    await InterfaceCheck().ConfigureAwait(false);
                }
                return true;
            }, () => 15000);
        }
        private async Task InterfaceCheck()
        {
            if (await InterfaceAvailable() == false && tuntapTransfer.Status != TuntapStatus.Operating)
            {
                LoggerHelper.Instance.Error($"tuntap inerface {tuntapTransfer.DeviceName} is down, restarting");
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
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == tuntapTransfer.DeviceName);
            return networkInterface != null && networkInterface.OperationalStatus == OperationalStatus.Up && await InterfacePing();
        }
        private async Task<bool> InterfacePing()
        {
            try
            {
                using Ping ping = new Ping();
                PingReply pingReply = await ping.SendPingAsync(runningConfig.Data.Tuntap.IP, 500);
                return pingReply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
