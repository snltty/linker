using linker.libs;
using System.Net;
using linker.tun;
using linker.libs.timer;
using static linker.nat.LinkerDstMapping;
using linker.tun.device;

namespace linker.messenger.tuntap
{
    public sealed class TuntapTransfer
    {
        private readonly LinkerTunDeviceAdapter linkerTunDeviceAdapter;

        private OperatingManager operatingManager = new OperatingManager();
        public TuntapStatus Status => operatingManager.Operating ? TuntapStatus.Operating : (TuntapStatus)(byte)linkerTunDeviceAdapter.Status;
        public string SetupError => linkerTunDeviceAdapter.SetupError;
        public string NatError => linkerTunDeviceAdapter.NatError;

        public bool AppNat => linkerTunDeviceAdapter.AppNat;

        public Action OnSetupBefore { get; set; } = () => { };
        public Action OnSetupAfter { get; set; } = () => { };
        public Action OnSetupSuccess { get; set; } = () => { };
        public Action OnShutdownBefore { get; set; } = () => { };
        public Action OnShutdownAfter { get; set; } = () => { };
        public Action OnShutdownSuccess { get; set; } = () => { };

        public TuntapTransfer(LinkerTunDeviceAdapter linkerTunDeviceAdapter)
        {
            this.linkerTunDeviceAdapter = linkerTunDeviceAdapter;
            Helper.OnAppExit += Helper_OnAppExit;
        }

        private void Helper_OnAppExit(object sender, EventArgs e)
        {
            Shutdown();
        }

        public void Initialize(ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            linkerTunDeviceAdapter.Initialize(linkerTunDeviceCallback);

        }
        public void Initialize(ILinkerTunDevice linkerTunDevice, ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            linkerTunDeviceAdapter.Initialize(linkerTunDevice, linkerTunDeviceCallback);
        }

        public async ValueTask<bool> Write(string srcId, ReadOnlyMemory<byte> buffer)
        {
            return await linkerTunDeviceAdapter.Write(srcId, buffer).ConfigureAwait(false);
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        public void Setup(LinkerTunDeviceSetupInfo info)
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            TimerHelper.Async(() =>
            {
                try
                {
                    
                    if (info.Address.Equals(IPAddress.Any))
                    {
                        return;
                    }
                    OnSetupBefore();
                    linkerTunDeviceAdapter.Setup(info);
                    if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.SetupError) == false)
                    {
                        LoggerHelper.Instance.Error(linkerTunDeviceAdapter.SetupError);
                        return;
                    }
                    OnSetupSuccess();
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

                    OnSetupAfter();
                    operatingManager.StopOperation();
                }
            });
        }

        /// <summary>
        /// 停止网卡
        /// </summary>
        public bool Shutdown()
        {
            if (operatingManager.StartOperation() == false)
            {
                return false;
            }
            try
            {
                OnShutdownBefore();
                linkerTunDeviceAdapter.Shutdown();
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
                OnShutdownAfter();
                operatingManager.StopOperation();
            }
            return true;
        }
        /// <summary>
        /// 刷新网卡
        /// </summary>
        public void Refresh()
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            TimerHelper.Async(() =>
            {
                try
                {
                    linkerTunDeviceAdapter.Refresh();
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
                }
            });
        }

        /// <summary>
        /// 设置NAT
        /// </summary>
        /// <param name="items"></param>
        public void SetNat(LinkerTunAppNatItemInfo[] items)
        {
            linkerTunDeviceAdapter.SetNat(items);
        }
        /// <summary>
        /// 移除NAT
        /// </summary>
        public void RemoveNat()
        {
            linkerTunDeviceAdapter.RemoveNat();
        }

        /// <summary>
        /// 添加转发
        /// </summary>
        /// <param name="forward"></param>
        public void AddForward(List<LinkerTunDeviceForwardItem> forward)
        {
            linkerTunDeviceAdapter.AddForward(forward);
        }
        /// <summary>
        /// 移除转发
        /// </summary>
        /// <param name="forward"></param>
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forward)
        {
            linkerTunDeviceAdapter.RemoveForward(forward);
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips"></param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips)
        {
            linkerTunDeviceAdapter.AddRoute(ips);
        }
        /// <summary>
        /// 移除路由
        /// </summary>
        /// <param name="ips"></param>
        public void RemoveRoute(LinkerTunDeviceRouteItem[] ips)
        {
            linkerTunDeviceAdapter.RemoveRoute(ips);
        }

        /// <summary>
        /// 添加映射
        /// </summary>
        /// <param name="maps"></param>
        public void SetDstMap(DstMapInfo[] maps)
        {
            linkerTunDeviceAdapter.SetMap(maps);
        }
        public void RemoveDstMap()
        {
            linkerTunDeviceAdapter.RemoveNat();
        }

        /// <summary>
        /// 检查网卡是否可用
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<bool> CheckAvailable(bool order = false)
        {
            return await linkerTunDeviceAdapter.CheckAvailable(order).ConfigureAwait(false);
        }
    }
}
