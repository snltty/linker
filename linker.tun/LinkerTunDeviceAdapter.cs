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

        private string error = string.Empty;
        public string Error => error;

        private string error1 = string.Empty;
        public string Error1 => error1;


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
                error = $"setup are operating";
                return false;
            }
            try
            {
                if (linkerTunDevice == null)
                {
                    error = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} not support";
                    return false;
                }
                linkerTunDevice.Setup(address, NetworkHelper.ToGatewayIP(address, prefixLength), prefixLength, out error);
                if (string.IsNullOrWhiteSpace(error) == false)
                {
                    return false;
                }
                linkerTunDevice.SetMtu(mtu);
                Read();
                return true;
            }
            catch (Exception ex)
            {
                error = ex + "";
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
                error = $"shutdown are operating";
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

            error = string.Empty;
            Interlocked.Exchange(ref operating, 0);

            return true;
        }

        /// <summary>
        /// 添加NAT转发,这会将来到本网卡且目标IP不是本网卡IP的包转发到其它网卡
        /// </summary>
        public void SetNat()
        {
            linkerTunDevice?.RemoveNat(out string error);
            linkerTunDevice?.SetNat(out error1);
        }
        /// <summary>
        /// 移除NAT转发
        /// </summary>
        public void RemoveNat()
        {
            linkerTunDevice?.RemoveNat(out string error);
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
        /// <param name="ips">路由IP</param>
        /// <param name="ip">网卡IP</param>
        /// <param name="gateway">是否网关，true添加NAT转发，false添加路由</param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            linkerTunDevice?.AddRoute(ips, ip);
        }
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ips">路由IP</param>
        /// <param name="gateway">是否网关，true删除NAT转发，false删除路由</param>
        public void DelRoute(LinkerTunDeviceRouteItem[] ips)
        {
            linkerTunDevice?.DelRoute(ips);
        }


        private void Read()
        {
            Task.Run(async () =>
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

                        await linkerTunDeviceCallback.Callback(packet).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
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
