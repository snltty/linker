using linker.libs;
using System.Net;

namespace linker.tun
{
    public sealed class LinkerTunDeviceAdapter
    {
        private ILinkerTunDevice linkerTunDevice;
        private ILinkerTunDeviceCallback linkerTunDeviceCallback;
        private CancellationTokenSource cancellationTokenSource;

        private string error = string.Empty;
        public string Error => error;


        private bool starting = false;
        public LinkerTunDeviceStatus Status
        {
            get
            {
                if (linkerTunDevice == null) return LinkerTunDeviceStatus.Normal;

                return linkerTunDevice.Running
                    ? LinkerTunDeviceStatus.Running
                    : starting
                        ? LinkerTunDeviceStatus.Starting
                        : LinkerTunDeviceStatus.Normal;
            }
        }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="linkerTunDeviceCallback">数据包回调</param>
        public LinkerTunDeviceAdapter()
        {
        }

        public void SetCallback(ILinkerTunDeviceCallback linkerTunDeviceCallback)
        {
            this.linkerTunDeviceCallback = linkerTunDeviceCallback;
        }


        /// <summary>
        /// 开启网卡
        /// </summary>
        /// <param name="name">网卡名，如果是osx，需要utunX的命名，X是一个数字</param>
        /// <param name="guid">windows的时候，需要一个固定guid，不然网卡编号一直递增，注册表一直新增记录</param>
        /// <param name="address">网卡IP</param>
        /// <param name="mask">掩码。一般24即可</param>
        public void SetUp(string name, Guid guid, IPAddress address, byte mask)
        {
            if (starting) return;
            Shutdown();
            starting = true;
            try
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
                }
                if (linkerTunDevice != null)
                {
                    linkerTunDevice.Shutdown();

                    linkerTunDevice.SetUp(address, NetworkHelper.ToGatewayIP(address, mask), mask, out error);
                    if (string.IsNullOrWhiteSpace(error))
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                        Task.Run(async () =>
                        {
                            while (cancellationTokenSource.IsCancellationRequested == false)
                            {
                                try
                                {
                                    ReadOnlyMemory<byte> buffer = linkerTunDevice.Read();
                                    if (buffer.Length == 0)
                                    {
                                        break;
                                    }

                                    LinkerTunDevicPacket packet = new LinkerTunDevicPacket();
                                    packet.Packet = buffer;
                                    packet.Version = (byte)(buffer.Span[0] >> 4 & 0b1111);

                                    if (packet.Version == 4)
                                    {
                                        packet.SourceIPAddress = buffer.Slice(12, 4);
                                        packet.DistIPAddress = buffer.Slice(16, 4);
                                    }
                                    else if (packet.Version == 6)
                                    {
                                        packet.SourceIPAddress = buffer.Slice(8, 16);
                                        packet.DistIPAddress = buffer.Slice(24, 16);
                                    }

                                    await linkerTunDeviceCallback.Callback(packet);
                                }
                                catch (Exception)
                                {
                                    break;
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                starting = false;
            }
        }
        /// <summary>
        /// 关闭网卡
        /// </summary>
        public void Shutdown()
        {
            cancellationTokenSource?.Cancel();
            if (linkerTunDevice != null)
            {
                linkerTunDevice.Shutdown();
            }

            error = string.Empty;
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        /// <param name="ips">路由记录，ip和掩码</param>
        /// <param name="ip">目标IP</param>
        public void AddRoute(LinkerTunDeviceRouteItem[] ips, IPAddress ip)
        {
            if (linkerTunDevice != null)
            {
                linkerTunDevice.AddRoute(ips, ip);
            }
        }
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ips">路由记录，ip和掩码</param>
        public void DelRoute(LinkerTunDeviceRouteItem[] ips)
        {
            if (linkerTunDevice != null)
            {
                linkerTunDevice.DelRoute(ips);
            }
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
        /// <param name="count">包头长度</param>
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
