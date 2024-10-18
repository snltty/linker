using linker.client.config;
using linker.libs;
using System.Net;
using System.Net.NetworkInformation;
using linker.plugins.client;
using linker.plugins.tuntap.config;
using linker.tun;

namespace linker.plugins.tuntap.client
{
    public sealed class TuntapTransfer
    {
        private readonly RunningConfig runningConfig;
        private readonly LinkerTunDeviceAdapter linkerTunDeviceAdapter;

        private string deviceName = "linker";
        private OperatingManager operatingManager = new OperatingManager();
        public TuntapStatus Status => operatingManager.Operating ? TuntapStatus.Operating : (TuntapStatus)(byte)linkerTunDeviceAdapter.Status;
        public string Error => linkerTunDeviceAdapter.Error;
        public string Error1 => linkerTunDeviceAdapter.Error1;

        public Action OnSetupBefore { get; set; } = () => { };
        public Action OnSetupAfter { get; set; } = () => { };
        public Action OnSetupSuccess { get; set; } = () => { };
        public Action OnShutdownBefore { get; set; } = () => { };
        public Action OnShutdownAfter { get; set; } = () => { };
        public Action OnShutdownSuccess { get; set; } = () => { };

        private ulong setupTimes = 0;

        public TuntapTransfer(ClientSignInState clientSignInState, LinkerTunDeviceAdapter linkerTunDeviceAdapter, TuntapProxy tuntapProxy, RunningConfig runningConfig)
        {
            this.linkerTunDeviceAdapter = linkerTunDeviceAdapter;
            this.runningConfig = runningConfig;

            linkerTunDeviceAdapter.Initialize(deviceName, tuntapProxy);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => linkerTunDeviceAdapter.Shutdown();
            Console.CancelKeyPress += (s, e) => linkerTunDeviceAdapter.Shutdown();
            

            CheckTuntapStatusTask();
        }
      
        /// <summary>
        /// 运行网卡
        /// </summary>
        public void Setup()
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            TimerHelper.Async(() =>
            {
                OnSetupBefore();
                try
                {
                    if (runningConfig.Data.Tuntap.IP.Equals(IPAddress.Any))
                    {
                        return;
                    }
                    linkerTunDeviceAdapter.Setup(runningConfig.Data.Tuntap.IP, runningConfig.Data.Tuntap.PrefixLength, 1400);
                    if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.Error))
                    {
                        setupTimes++;
                        linkerTunDeviceAdapter.SetNat();
                        OnSetupSuccess();
                    }
                    else
                    {
                        LoggerHelper.Instance.Error(linkerTunDeviceAdapter.Error);
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
                finally
                {
                    operatingManager.StopOperation();
                    OnSetupAfter();
                }
            });
        }

        /// <summary>
        /// 停止网卡
        /// </summary>
        public void Shutdown()
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            try
            {
                OnShutdownBefore();
                linkerTunDeviceAdapter.Shutdown();
                linkerTunDeviceAdapter.RemoveNat();
                linkerTunDeviceAdapter.Clear();
                OnShutdownSuccess();
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                operatingManager.StopOperation();
                OnShutdownAfter();
            }
        }

        public void AddForward(List<LinkerTunDeviceForwardItem> forward)
        {
            linkerTunDeviceAdapter.AddForward(forward);
        }
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forward)
        {
            linkerTunDeviceAdapter.RemoveForward(forward);
        }

        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            linkerTunDeviceAdapter.AddRoute(ips, ip);
        }
        public void DelRoute(LinkerTunDeviceRouteItem[] ips)
        {
            linkerTunDeviceAdapter.DelRoute(ips);
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
            }, 15000);
        }
        private async Task InterfaceCheck()
        {
            if (await InterfaceAvailable() == false && operatingManager.Operating == false)
            {
                LoggerHelper.Instance.Error($"tuntap inerface {deviceName} is down, restarting");
                linkerTunDeviceAdapter.Shutdown();
                await Task.Delay(5000).ConfigureAwait(false);
                if (await InterfaceAvailable() == false && operatingManager.Operating == false)
                {
                    Setup();
                }
            }
        }
        private async Task<bool> InterfaceAvailable()
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == deviceName);
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
