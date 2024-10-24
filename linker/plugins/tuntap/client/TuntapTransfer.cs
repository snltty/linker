using linker.client.config;
using linker.libs;
using System.Net;
using linker.plugins.client;
using linker.plugins.tuntap.config;
using linker.tun;

namespace linker.plugins.tuntap.client
{
    public sealed class TuntapTransfer
    {
        private readonly LinkerTunDeviceAdapter linkerTunDeviceAdapter;

        private OperatingManager operatingManager = new OperatingManager();
        public TuntapStatus Status => operatingManager.Operating ? TuntapStatus.Operating : (TuntapStatus)(byte)linkerTunDeviceAdapter.Status;
        public string SetupError => linkerTunDeviceAdapter.SetupError;
        public string NatError => linkerTunDeviceAdapter.NatError;

        public string DeviceName => "linker";

        public Action OnSetupBefore { get; set; } = () => { };
        public Action OnSetupAfter { get; set; } = () => { };
        public Action OnSetupSuccess { get; set; } = () => { };
        public Action OnShutdownBefore { get; set; } = () => { };
        public Action OnShutdownAfter { get; set; } = () => { };
        public Action OnShutdownSuccess { get; set; } = () => { };

        public TuntapTransfer(ClientSignInState clientSignInState, LinkerTunDeviceAdapter linkerTunDeviceAdapter, TuntapProxy tuntapProxy, RunningConfig runningConfig)
        {
            this.linkerTunDeviceAdapter = linkerTunDeviceAdapter;

            linkerTunDeviceAdapter.Initialize(DeviceName, tuntapProxy);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => linkerTunDeviceAdapter.Shutdown();
            Console.CancelKeyPress += (s, e) => linkerTunDeviceAdapter.Shutdown();
        }
      
        /// <summary>
        /// 运行网卡
        /// </summary>
        public void Setup(IPAddress ip,byte prefixLength)
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
                    if (ip.Equals(IPAddress.Any))
                    {
                        return;
                    }
                    linkerTunDeviceAdapter.Setup(ip, prefixLength, 1400);
                    if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.SetupError))
                    {
                        linkerTunDeviceAdapter.SetNat();
                        OnSetupSuccess();
                    }
                    else
                    {
                        LoggerHelper.Instance.Error(linkerTunDeviceAdapter.SetupError);
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

    }
}
