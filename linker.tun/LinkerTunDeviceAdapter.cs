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

        private uint operating = 0;
        public LinkerTunDeviceStatus Status
        {
            get
            {
                if (linkerTunDevice == null) return LinkerTunDeviceStatus.Normal;

                return operating == 1
                    ? LinkerTunDeviceStatus.Starting
                    : linkerTunDevice.Running
                        ? LinkerTunDeviceStatus.Running 
                        : LinkerTunDeviceStatus.Normal;
            }
        }

        public LinkerTunDeviceAdapter()
        {
        }

        /// <summary>
        /// 设置网卡读取回调
        /// </summary>
        /// <param name="linkerTunDeviceCallback"></param>
        public void SetReadCallback(ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            this.linkerTunDeviceCallback = linkerTunDeviceCallback;
        }

        /// <summary>
        /// 开启网卡
        /// </summary>
        /// <param name="name">网卡名，如果是osx，需要utunX的命名，X是一个数字</param>
        /// <param name="guid">windows的时候，需要一个固定guid，不然网卡编号一直递增，注册表一直新增记录</param>
        /// <param name="address">网卡IP</param>
        /// <param name="prefixLength">掩码。一般24即可</param>
        public bool SetUp(string name, Guid guid, IPAddress address, byte prefixLength)
        {
            if (Interlocked.CompareExchange(ref operating, 1, 0) == 1)
            {
                error = $"setup are operating";
                return false;
            }
            try
            {
                InitInstance(name, guid);
                if (linkerTunDevice == null)
                {
                    error = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} not support";
                    return false;
                }
                linkerTunDevice.SetUp(address, NetworkHelper.ToGatewayIP(address, prefixLength), prefixLength, out error);
                if (string.IsNullOrWhiteSpace(error) == false)
                {
                    return false;
                }
                Read();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                Interlocked.Exchange(ref operating, 0);
            }
            return false;
        }
        private void InitInstance(string name, Guid guid)
        {
            if (linkerTunDevice == null)
            {
                if (OperatingSystem.IsWindows())
                {
                    linkerTunDevice = new LinkerWinTunDevice(name, guid);
                }
                else if (OperatingSystem.IsLinux())
                {
                    linkerTunDevice = new LinkerLinuxTunDevice(name);
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
        /// 关闭网卡
        /// </summary>
        public bool Shutdown(int index)
        {
            if (Interlocked.CompareExchange(ref operating, 1, 0) == 1)
            {
                error = $"shutdown are operating";
                return false;
            }
            cancellationTokenSource?.Cancel();
            linkerTunDevice?.Shutdown();
            linkerTunDevice?.RemoveNat();

            error = string.Empty;
            Interlocked.Exchange(ref operating, 0);

            return true;
        }

        /// <summary>
        /// 设置MTU
        /// </summary>
        /// <param name="value"></param>
        public void SetMtu(int value)
        {
            linkerTunDevice?.SetMtu(value);
        }
        /// <summary>
        /// 添加NAT转发
        /// </summary>
        public void SetNat()
        {
            linkerTunDevice?.SetNat();
        }
        /// <summary>
        /// 移除NAT转发
        /// </summary>
        public void RemoveNat()
        {
            linkerTunDevice?.RemoveNat();
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips">路由记录，ip和掩码</param>
        /// <param name="ip">目标IP</param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            linkerTunDevice?.AddRoute(ips, ip);
        }
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ips">路由记录，ip和掩码</param>
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
                            Shutdown(4);
                            break;
                        }


                        LinkerTunDevicPacket packet = new LinkerTunDevicPacket();
                        packet.Packet = buffer;
                        packet.IPPacket = buffer.Slice(4);

                        packet.Version = (byte)(packet.IPPacket.Span[0] >> 4 & 0b1111);

                        if (packet.Version == 4)
                        {
                            packet.SourceIPAddress = packet.IPPacket.Slice(12, 4);
                            packet.DistIPAddress = packet.IPPacket.Slice(16, 4);
                        }
                        else if (packet.Version == 6)
                        {
                            packet.SourceIPAddress = packet.IPPacket.Slice(8, 16);
                            packet.DistIPAddress = packet.IPPacket.Slice(24, 16);
                        }
                        await linkerTunDeviceCallback.Callback(packet).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        Shutdown(5);
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
