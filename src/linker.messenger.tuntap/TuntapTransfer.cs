using linker.libs;
using System.Net;
using linker.tun;
using linker.libs.timer;

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
            if (OperatingSystem.IsAndroid() == false)
            {
                AppDomain.CurrentDomain.ProcessExit += (s, e) => linkerTunDeviceAdapter.Shutdown();
                Console.CancelKeyPress += (s, e) => linkerTunDeviceAdapter.Shutdown();
            }
        }

        public void Initialize(ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            linkerTunDeviceAdapter.Initialize(linkerTunDeviceCallback);

        }
        public void Initialize(ILinkerTunDevice linkerTunDevice, ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            linkerTunDeviceAdapter.Initialize(linkerTunDevice, linkerTunDeviceCallback);
        }

        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            return linkerTunDeviceAdapter.Write(buffer);
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        public void Setup(string name, IPAddress ip, byte prefixLength, bool nat = true)
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            TimerHelper.Async(() =>
            {
                try
                {
                    if (ip.Equals(IPAddress.Any))
                    {
                        return;
                    }
                    OnSetupBefore();
                    linkerTunDeviceAdapter.Setup(name, ip, prefixLength, 1420);
                    if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.SetupError) == false)
                    {
                        LoggerHelper.Instance.Error(linkerTunDeviceAdapter.SetupError);
                        return;
                    }
                    if (nat)
                    {
                        linkerTunDeviceAdapter.SetSystemNat();
                        if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.NatError) == false)
                        {
                            LoggerHelper.Instance.Error(linkerTunDeviceAdapter.NatError);
                        }
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
        public void Shutdown(bool notify = true)
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            try
            {
                if (notify) OnShutdownBefore();
                linkerTunDeviceAdapter.Shutdown();
                if (notify) OnShutdownSuccess();
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
                if (notify)
                    OnShutdownAfter();
                operatingManager.StopOperation();
            }
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
        /// 设置应用层NAT
        /// </summary>
        /// <param name="items"></param>
        public void SetAppNat(LinkerTunAppNatItemInfo[] items)
        {
            if (string.IsNullOrWhiteSpace(NatError) == false)
            {
                linkerTunDeviceAdapter.SetAppNat(items);
            }
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
        public void SetMap(LanMapInfo[] maps)
        {
            linkerTunDeviceAdapter.SetMap(maps);
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
