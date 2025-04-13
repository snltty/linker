using linker.libs;
using linker.libs.timer;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq;
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


        private FrozenDictionary<uint, uint> mapDic = new Dictionary<uint, uint>().ToFrozenDictionary();
        private uint[] masks = Array.Empty<uint>();


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
        private unsafe void MapToRealIP(ReadOnlyMemory<byte> buffer)
        {
            //只支持映射IPV4
            if ((byte)(buffer.Span[0] >> 4 & 0b1111) != 4) return;
            //映射表不为空
            if (masks.Length == 0 || mapDic.Count == 0) return;

            uint dist = BinaryPrimitives.ReadUInt32BigEndian(buffer.Span.Slice(16, 4));
            for (int i = 0; i < masks.Length; i++)
            {
                //目标IP网络号存在映射表中，找到映射后的真实网络号，替换网络号得到最终真实的IP
                if (mapDic.TryGetValue(dist & masks[i], out uint realNetwork))
                {
                    //将原本的目标IP修改为映射的IP
                    fixed (byte* ptr = buffer.Span)
                    {
                        //修改目标IP
                        *(uint*)(ptr + 16) = BinaryPrimitives.ReverseEndianness(realNetwork | (dist & ~masks[i]));
                        //重新计算IP头校验和
                        *(ushort*)(ptr + 10) = 0;
                        *(ushort*)(ptr + 10) = Checksum((ushort*)ptr, (byte)((*ptr & 0b1111) * 4));
                    }
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
                return;
            }

            mapDic = maps.ToFrozenDictionary(x => NetworkHelper.ToNetworkValue(x.IP, x.PrefixLength), x => NetworkHelper.ToNetworkValue(x.ToIP, x.PrefixLength));
            masks = maps.Select(x => NetworkHelper.ToPrefixValue(x.PrefixLength)).ToArray();
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
