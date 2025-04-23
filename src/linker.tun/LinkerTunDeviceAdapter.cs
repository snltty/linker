using linker.libs;
using linker.libs.timer;
using linker.snat;
using System.Net;
using static linker.snat.LinkerDstMapping;

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

        public bool AppNat => linkerTunDevice?.AppNat ?? false;
        private readonly LinkerDstMapping linkerDstMapping = new LinkerDstMapping();

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

        public LinkerTunDeviceAdapter()
        {

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
        /// <param name="deviceName">网卡IP</param>
        /// <param name="address">网卡IP</param>
        /// <param name="prefixLength">掩码。一般24即可</param>
        /// <param name="mtu">mtu</param>
        public bool Setup(string deviceName, IPAddress address, byte prefixLength, int mtu)
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
                linkerTunDevice.Setup(deviceName, address, prefixLength, out setupError);
                if (string.IsNullOrWhiteSpace(setupError) == false)
                {
                    return false;
                }
                linkerTunDevice.SetMtu(mtu);
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
                linkerTunDevice.RemoveNat(out string error);
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
        public void SetSystemNat()
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            if (linkerTunDevice.Running)
                linkerTunDevice.SetSystemNat(out natError);
        }
        /// <summary>
        /// 设置应用层NAT，仅Windows，
        /// 目录下
        /// 64位，放x64的WinDivert.dll和WinDivert64.sys
        /// 32位，放x86的WinDivert.dll和WinDivert64.sys，WinDivert.sys
        /// </summary>
        /// <param name="items"></param>
        public void SetAppNat(LinkerTunAppNatItemInfo[] items)
        {
            if (linkerTunDevice == null)
            {
                return;
            }
            if (linkerTunDevice.Running)
                linkerTunDevice.SetAppNat(items, ref natError);
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
            linkerTunDevice.RemoveNat(out string error);
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

        private void Read()
        {
            TimerHelper.Async(async () =>
            {
                cancellationTokenSource = new CancellationTokenSource();
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

                        LinkerTunDevicPacket packet = new LinkerTunDevicPacket(buffer, 0, length);
                        if (packet.DistIPAddress.Length == 0) continue;

                        linkerDstMapping.ToFakeDst(buffer.AsMemory(4, length - 4));
                        await linkerTunDeviceCallback.Callback(packet).ConfigureAwait(false);
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
        /// 写入网卡
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            if (linkerTunDevice == null || Status != LinkerTunDeviceStatus.Running) return false;

            linkerDstMapping.ToRealDst(buffer, AppNat == false);
            return linkerTunDevice.Write(buffer);
        }


        /// <summary>
        /// 设置IP映射列表
        /// </summary>
        /// <param name="maps"></param>
        public void SetMap(DstMapInfo[] maps)
        {
            linkerDstMapping.SetDsts(maps);
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
