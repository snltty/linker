using linker.libs;
using linker.libs.timer;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Net;
using System.Net.Sockets;

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


        private FrozenDictionary<uint, uint> mapDic = new Dictionary<uint, uint>().ToFrozenDictionary();
        private uint[] masks = Array.Empty<uint>();

        private ConcurrentDictionary<uint, uint> natDic = new ConcurrentDictionary<uint, uint>();


        private OperatingManager operatingManager = new OperatingManager();
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
                linkerTunDevice.Setup(deviceName, address, NetworkHelper.ToGatewayIP(address, prefixLength), prefixLength, out setupError);
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
            if (operatingManager.StartOperation() == false)
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
            linkerTunDevice?.Refresh();
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
                        byte[] buffer = linkerTunDevice.Read(out int length);
                        if (length == 0)
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                LoggerHelper.Instance.Warning($"tuntap read buffer 0");
                            await Task.Delay(1000);
                            continue;
                        }

                        LinkerTunDevicPacket packet = new LinkerTunDevicPacket();
                        packet.Unpacket(buffer, 0, length);
                        if (packet.DistIPAddress.Length == 0) continue;

                        ToMapIP(buffer.AsMemory(4, length - 4));
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
        public unsafe bool Write(ReadOnlyMemory<byte> buffer)
        {
            if (linkerTunDevice != null && Status == LinkerTunDeviceStatus.Running)
            {
                MapToRealIP(buffer);
                return linkerTunDevice.Write(buffer);
            }
            return false;
        }

        private unsafe void ReWriteIP(ReadOnlyMemory<byte> buffer, uint newIP, int pos)
        {
            fixed (byte* ptr = buffer.Span)
            {
                byte ipHeaderLength = (byte)((*ptr & 0b1111) * 4);

                //修改目标IP
                *(uint*)(ptr + pos) = newIP;
                //重新计算IP头校验和
                *(ushort*)(ptr + 10) = 0;
                *(ushort*)(ptr + 10) = BinaryPrimitives.ReverseEndianness(Checksum((ushort*)ptr, ipHeaderLength));

                ProtocolType protocol = (ProtocolType)buffer.Span[9];
                switch (protocol)
                {
                    case ProtocolType.Tcp:
                        {
                            *(ushort*)(ptr + ipHeaderLength + 16) = 0;
                            ulong sum = PseudoHeaderSum(ptr, (uint)(buffer.Length - ipHeaderLength));
                            ushort checksum = Checksum((ushort*)(ptr + ipHeaderLength), (uint)buffer.Length - ipHeaderLength, sum);
                            *(ushort*)(ptr + ipHeaderLength + 16) = BinaryPrimitives.ReverseEndianness(checksum);
                        }
                        break;
                    case ProtocolType.Udp:
                        {
                            *(ushort*)(ptr + ipHeaderLength + 6) = 0;
                            ulong sum = PseudoHeaderSum(ptr, (uint)(buffer.Length - ipHeaderLength));
                            ushort checksum = Checksum((ushort*)(ptr + ipHeaderLength), (uint)buffer.Length - ipHeaderLength, sum);
                            *(ushort*)(ptr + ipHeaderLength + 6) = BinaryPrimitives.ReverseEndianness(checksum);
                        }
                        break;
                }
            }
        }
        private void ToMapIP(ReadOnlyMemory<byte> buffer)
        {
            //只支持映射IPV4
            if ((byte)(buffer.Span[0] >> 4 & 0b1111) != 4) return;
            //映射表不为空
            if (natDic.IsEmpty) return;

            uint realDist = NetworkHelper.ToValue(buffer.Span.Slice(12, 4));
            if (natDic.TryGetValue(realDist, out uint fakeDist))
            {
                ReWriteIP(buffer, BinaryPrimitives.ReverseEndianness( fakeDist), 12);
            }
        }
        private void MapToRealIP(ReadOnlyMemory<byte> buffer)
        {
            //只支持映射IPV4
            if ((byte)(buffer.Span[0] >> 4 & 0b1111) != 4) return;
            //映射表不为空
            if (masks.Length == 0 || mapDic.Count == 0) return;

            uint fakeDist = NetworkHelper.ToValue(buffer.Span.Slice(16, 4));
            for (int i = 0; i < masks.Length; i++)
            {
                //目标IP网络号存在映射表中，找到映射后的真实网络号，替换网络号得到最终真实的IP
                if (mapDic.TryGetValue(fakeDist & masks[i], out uint realNetwork))
                {
                    uint realDist = realNetwork | (fakeDist & ~masks[i]);
                    ReWriteIP(buffer, BinaryPrimitives.ReverseEndianness(realDist), 16);
                    natDic.AddOrUpdate(realDist, fakeDist, (a, b) => fakeDist);
                    break;
                }
            }
        }
        /// <summary>
        /// 设置IP映射列表
        /// </summary>
        /// <param name="maps"></param>
        public void SetMap(LanMapInfo[] maps)
        {
            if (maps == null || maps.Length == 0)
            {
                mapDic = new Dictionary<uint, uint>().ToFrozenDictionary();
                masks = Array.Empty<uint>();
                natDic.Clear();
                return;
            }

            mapDic = maps.ToFrozenDictionary(x => NetworkHelper.ToNetworkValue(x.IP, x.PrefixLength), x => NetworkHelper.ToNetworkValue(x.ToIP, x.PrefixLength));
            masks = maps.Select(x => NetworkHelper.ToPrefixValue(x.PrefixLength)).ToArray();
        }
        /// <summary>
        /// 计算校验和
        /// </summary>
        /// <param name="addr">包头开始位置</param>
        /// <param name="length">计算长度，不同协议不同长度，请自己斟酌</param>
        /// <param name="pseudoHeaderSum">伪头部和，默认0不需要伪头部和</param>
        /// <returns></returns>
        public unsafe ushort Checksum(ushort* addr, uint length, ulong pseudoHeaderSum = 0)
        {
            //每两个字节为一个数，之和
            while (length > 1)
            {
                pseudoHeaderSum += (ushort)((*addr >> 8) + (*addr << 8));
                addr++;
                length -= 2;
            }
            //奇数字节末尾补零
            if (length > 0) pseudoHeaderSum += (ushort)((*addr) << 8);
            //溢出处理
            while ((pseudoHeaderSum >> 16) != 0) pseudoHeaderSum = (pseudoHeaderSum & 0xffff) + (pseudoHeaderSum >> 16);
            //取反
            return (ushort)(~pseudoHeaderSum);
        }
        /// <summary>
        /// 计算伪头部和，如TCP/UDP校验和需要一个伪头部
        /// </summary>
        /// <param name="addr">IP包头开始</param>
        /// <param name="length">TCP/UDP长度</param>
        /// <returns></returns>
        public unsafe ulong PseudoHeaderSum(byte* addr, uint length)
        {
            uint sum = 0;
            //源IP+目的IP
            for (byte i = 12; i < 20; i += 2) sum += (uint)((*(addr + i) << 8) | *(addr + i + 1));
            //协议
            sum += *(addr + 9);
            //协议内容长度
            sum += length;
            return sum;
        }


        public async Task<bool> CheckAvailable(bool order = false)
        {
            return await linkerTunDevice.CheckAvailable(order);
        }
    }

    public sealed class LanMapInfo
    {
        public IPAddress IP { get; set; }
        public IPAddress ToIP { get; set; }
        public byte PrefixLength { get; set; }
    }
}
