using linker.libs;
using System.Net;

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


        private uint operating = 0;
        public LinkerTunDeviceStatus Status
        {
            get
            {
                if (linkerTunDevice == null) return LinkerTunDeviceStatus.Normal;

                return operating == 1
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
        /// <param name="deviceName">设备名</param>
        /// <param name="linkerTunDeviceCallback">读取数据回调</param>
        public void Initialize(string deviceName, ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            this.linkerTunDeviceCallback = linkerTunDeviceCallback;
            if (linkerTunDevice == null)
            {
                if (OperatingSystem.IsWindows())
                {
                    linkerTunDevice = new LinkerWinTunDevice(deviceName, Guid.NewGuid());
                }
                else if (OperatingSystem.IsLinux())
                {
                    linkerTunDevice = new LinkerLinuxTunDevice(deviceName);
                }
                /*
                else if (OperatingSystem.IsMacOS())
                {
                    linkerTunDevice = new LinkerOsxTunDevice("utun12138");
                }
                */
            }
        }

        /// <summary>
        /// 清理额外的数据，具体看不同平台的实现
        /// </summary>
        public void Clear()
        {
            linkerTunDevice?.Clear();
        }


        /// <summary>
        /// 开启网卡
        /// </summary>
        /// <param name="address">网卡IP</param>
        /// <param name="prefixLength">掩码。一般24即可</param>
        /// <param name="mtu">mtu</param>
        public bool Setup(IPAddress address, byte prefixLength, int mtu)
        {
            if (Interlocked.CompareExchange(ref operating, 1, 0) == 1)
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
                linkerTunDevice.Setup(address, NetworkHelper.ToGatewayIP(address, prefixLength), prefixLength, out setupError);
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
                setupError = ex + "";
            }
            finally
            {
                Interlocked.Exchange(ref operating, 0);
            }
            return false;
        }

        /// <summary>
        /// 关闭网卡
        /// </summary>
        public bool Shutdown()
        {
            if (Interlocked.CompareExchange(ref operating, 1, 0) == 1)
            {
                setupError = $"shutdown are operating";
                return false;
            }
            try
            {
                cancellationTokenSource?.Cancel();
                linkerTunDevice?.Shutdown();
            }
            catch (Exception)
            {
            }

            setupError = string.Empty;
            Interlocked.Exchange(ref operating, 0);

            return true;
        }

        /// <summary>
        /// 添加NAT转发,这会将来到本网卡且目标IP不是本网卡IP的包转发到其它网卡
        /// </summary>
        public void SetNat()
        {
            linkerTunDevice?.RemoveNat(out string error);
            linkerTunDevice?.SetNat(out natError);
        }
        /// <summary>
        /// 移除NAT转发
        /// </summary>
        public void RemoveNat()
        {
            linkerTunDevice?.RemoveNat(out string error);
        }

        /// <summary>
        /// 获取端口转发
        /// </summary>
        /// <returns></returns>
        public List<LinkerTunDeviceForwardItem> GetForward()
        {
            return linkerTunDevice?.GetForward() ?? [];
        }
        /// <summary>
        /// 添加端口转发
        /// </summary>
        /// <param name="forwards"></param>
        public void AddForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            linkerTunDevice?.AddForward(forwards);
        }
        /// <summary>
        /// 移除端口转发
        /// </summary>
        /// <param name="forwards"></param>
        public void RemoveForward(List<LinkerTunDeviceForwardItem> forwards)
        {
            linkerTunDevice?.RemoveForward(forwards);
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips"></param>
        /// <param name="ip"></param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            linkerTunDevice?.AddRoute(ips, ip);
        }
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ips"></param>
        public void DelRoute(LinkerTunDeviceRouteItem[] ips)
        {
            linkerTunDevice?.DelRoute(ips);
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
                        ReadOnlyMemory<byte> buffer = linkerTunDevice.Read();
                        if (buffer.Length == 0)
                        {
                            Shutdown();
                            break;
                        }


                        LinkerTunDevicPacket packet = new LinkerTunDevicPacket();
                        packet.Unpacket(buffer);

                        try
                        {
                            await linkerTunDeviceCallback.Callback(packet).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            setupError = ex.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        setupError = ex.Message;
                        Shutdown();
                        break;
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
            if (linkerTunDevice != null)
            {
                return linkerTunDevice.Write(buffer);
            }
            return false;
        }

        /// <summary>
        /// 计算校验和
        /// </summary>
        /// <param name="addr">包头开始位置</param>
        /// <param name="count">长度,IP包仅包头，ICMP包则全部</param>
        /// <returns></returns>
        public unsafe ushort Checksum(ushort* addr, uint count)
        {
            ulong sum = 0;
            while (count > 1)
            {
                sum += *addr++;
                count -= 2;
            }
            if (count > 0)
            {
                sum += (ulong)((*addr) & ((((0xff00) & 0xff) << 8) | (((0xff00) & 0xff00) >> 8)));
            }
            while ((sum >> 16) != 0)
            {
                sum = (sum & 0xffff) + (sum >> 16);
            }
            sum = ~sum;
            return ((ushort)sum);
        }
    }
}
