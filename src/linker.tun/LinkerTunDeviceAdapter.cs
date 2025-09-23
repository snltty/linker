using linker.libs;
using linker.libs.timer;
using linker.tun.device;
using linker.tun.hook;
using System.Net;
using static linker.nat.LinkerDstMapping;

namespace linker.tun
{
    /// <summary>
    /// linker tun网卡适配器，自动选择不同平台的实现
    /// </summary>
    public sealed class LinkerTunDeviceAdapter
    {
        private ILinkerTunDevice linkerTunDevice;
        private ILinkerTunDeviceCallback linkerTunDeviceCallback;
        private CancellationTokenSource cancellationTokenSource;

        private string setupError = string.Empty;
        public string SetupError => setupError;

        private string natError = string.Empty;
        public string NatError => natError;
        public bool AppNat => lanDnat.Running;

        private IPAddress address;
        private byte prefixLength;
        private readonly LinkerTunPacketHookLanMap lanMap = new LinkerTunPacketHookLanMap();
        private readonly LinkerTunPacketHookLanDstProxy lanDnat = new LinkerTunPacketHookLanDstProxy();


        private readonly OperatingManager operatingManager = new OperatingManager();
        public LinkerTunDeviceStatus Status
        {
            get
            {
                if (linkerTunDevice == null) return LinkerTunDeviceStatus.Normal;

                return operatingManager.Operating
                    ? LinkerTunDeviceStatus.Operating
                    : linkerTunDevice.Running
                        ? LinkerTunDeviceStatus.Running
                        : LinkerTunDeviceStatus.Normal;
            }
        }

        private ILinkerTunPacketHook[] hooks = [];
        private ILinkerTunPacketHook[] hooks1 = [];

        public LinkerTunDeviceAdapter()
        {
            hooks = new ILinkerTunPacketHook[] { lanMap, lanDnat };
            hooks1 = hooks.OrderByDescending(c => c.Level).ToArray();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="linkerTunDeviceCallback">读取数据回调</param>
        public bool Initialize(ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            this.linkerTunDeviceCallback = linkerTunDeviceCallback;
            if (linkerTunDevice == null)
            {
                if (OperatingSystem.IsWindows())
                {
                    linkerTunDevice = new LinkerWinTunDevice();
                    return true;
                }
                else if (OperatingSystem.IsLinux())
                {
                    linkerTunDevice = new LinkerLinuxTunDevice();
                    return true;
                }
                /*
                else if (OperatingSystem.IsMacOS())
                {
                    linkerTunDevice = new LinkerOsxTunDevice("utun12138");
                }
                */
            }
            return false;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="linkerTunDevice">网卡实现</param>
        /// <param name="linkerTunDeviceCallback">读取数据回调</param>
        /// <returns></returns>
        public bool Initialize(ILinkerTunDevice linkerTunDevice, ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            this.linkerTunDevice = linkerTunDevice;
            this.linkerTunDeviceCallback = linkerTunDeviceCallback;
            return true;
        }

        /// <summary>
        /// 开启网卡
        /// </summary>
        /// <param name="info">网卡信息</param>
        public bool Setup(LinkerTunDeviceSetupInfo info)
        {
            if (operatingManager.StartOperation() == false)
            {
                setupError = $"setup are operating";
                return false;
            }
            try
            {
                if (linkerTunDevice == null)
                {
                    setupError = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} not support";
                    return false;
                }
                this.address = info.Address;
                this.prefixLength = info.PrefixLength;
                linkerTunDevice.Setup(info, out setupError);
                if (string.IsNullOrWhiteSpace(setupError) == false)
                {
                    return false;
                }
                linkerTunDevice.SetMtu(info.Mtu);
                Read();
                return true;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning($"tuntap setup Exception {ex}");
                setupError = ex.Message;
            }
            finally
            {
                operatingManager.StopOperation();
            }
            return false;
        }
        /// <summary>
        /// 关闭网卡
        /// </summary>
        public bool Shutdown()
        {
            if (linkerTunDevice == null)
            {
                return false;
            }
            if (operatingManager.StartOperation() == false)
            {
                setupError = $"shutdown are operating";
                return false;
            }
            try
            {
                cancellationTokenSource?.Cancel();
                linkerTunDevice.Shutdown();
            }
            catch (Exception)
            {
            }
            finally
            {
                operatingManager.StopOperation();
            }
            setupError = string.Empty;
            return true;
        }
        /// <summary>
        /// 刷新网卡
        /// </summary>
        public void Refresh()
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            linkerTunDevice.Refresh();
        }

        /// <summary>
        /// 设置系统层NAT
        /// </summary>
        public void SetNat(LinkerTunAppNatItemInfo[] items)
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            if (linkerTunDevice.Running)
            {
                linkerTunDevice.SetNat(out natError);
                lanDnat.Setup(address, prefixLength, items, ref natError);
            }
        }
        /// <summary>
        /// 移除NAT
        /// </summary>
        public void RemoveNat()
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            natError = string.Empty;
            linkerTunDevice.RemoveNat(out string error);
            lanDnat.Shutdown();
        }

        /// <summary>
        /// 获取端口转发
        /// </summary>
        /// <returns></returns>
        public List<LinkerTunDeviceForwardItem> GetForward()
        {
            if (linkerTunDevice == null)
            {
                return [];
            }
            return linkerTunDevice.GetForward();
        }
        /// <summary>
        /// 添加端口转发
        /// </summary>
        /// <param name="forwards"></param>
        public void AddForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            linkerTunDevice.AddForward(forwards);
        }
        /// <summary>
        /// 移除端口转发
        /// </summary>
        /// <param name="forwards"></param>
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            linkerTunDevice.RemoveForward(forwards);
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips"></param>
        /// <param name="ip"></param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips)
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            if (linkerTunDevice.Running)
                linkerTunDevice.AddRoute(ips);
        }
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ips"></param>
        public void RemoveRoute(LinkerTunDeviceRouteItem[] ips)
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            linkerTunDevice.RemoveRoute(ips);
        }

        public void AddHooks(List<ILinkerTunPacketHook> hooks)
        {
            List<ILinkerTunPacketHook> list = this.hooks.ToList();
            list.AddRange(hooks);

            this.hooks = list.Distinct().OrderBy(c => c.Level).ToArray();
            hooks1 = this.hooks.OrderByDescending(c => c.Level).ToArray();
        }

        private void Read()
        {
            TimerHelper.Async(async () =>
            {
                cancellationTokenSource = new CancellationTokenSource();
                LinkerTunDevicPacket packet = new LinkerTunDevicPacket();
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        byte[] buffer = linkerTunDevice.Read(out int length);
                        if (length == 0)
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                LoggerHelper.Instance.Warning($"tuntap read buffer 0");
                            await Task.Delay(1000);
                            continue;
                        }

                        packet.Unpacket(buffer, 0, length);
                        if (packet.DistIPAddress.Length == 0 || packet.Version != 4) continue;

                        for (int i = 0; i < hooks1.Length; i++) if (hooks1[i].Read(packet.IPPacket) == false) goto end;
                        ChecksumHelper.ChecksumWithZero(packet.IPPacket);

                        await linkerTunDeviceCallback.Callback(packet).ConfigureAwait(false);

                    end:;
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Warning($"tuntap read buffer Exception {ex}");
                        setupError = ex.Message;
                        await Task.Delay(1000);
                    }
                }
            });
        }
        /// <summary>
        /// 写入一个TCP/IP数据包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool Write(string srcId, ReadOnlyMemory<byte> buffer)
        {
            if (linkerTunDevice == null || Status != LinkerTunDeviceStatus.Running || new LinkerTunDevicValidatePacket(buffer).IsValid == false) return false;

            for (int i = 0; i < hooks.Length; i++) if (hooks[i].Write(srcId, buffer) == false) return false;
            ChecksumHelper.ChecksumWithZero(buffer);

            return linkerTunDevice.Write(buffer);
        }

        /// <summary>
        /// 设置IP映射列表
        /// </summary>
        /// <param name="maps"></param>
        public void SetMap(DstMapInfo[] maps)
        {
            lanMap.SetMap(maps);
        }
        /// <summary>
        /// 移除映射
        /// </summary>
        public void RemoveMap()
        {
            lanMap.SetMap([]);
        }

        public async Task<bool> CheckAvailable(bool order = false)
        {
            if (linkerTunDevice == null)
            {
                return false;
            }
            return await linkerTunDevice.CheckAvailable(order);
        }
    }


}
