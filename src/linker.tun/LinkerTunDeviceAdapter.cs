using linker.libs;
using linker.libs.timer;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Frozen;
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

        public bool AppNat=> linkerTunDevice?.AppNat ?? false;


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
            if (operatingManager.StartOperation() == false)
            {
                setupError = $"shutdown are operating";
                return false;
            }
            try
            {
                cancellationTokenSource?.Cancel();
                linkerTunDevice?.Shutdown();
                linkerTunDevice?.RemoveNat(out string error);
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
        /// 设置系统层NAT
        /// </summary>
        public void SetSystemNat()
        {
            linkerTunDevice?.SetSystemNat(out natError);
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
            linkerTunDevice?.SetAppNat(items, out natError);
        }
        /// <summary>
        /// 移除NAT
        /// </summary>
        public void RemoveNat()
        {
            linkerTunDevice.RemoveNat(out string error);
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
        public void AddRoute(LinkerTunDeviceRouteItem[] ips)
        {
            linkerTunDevice?.AddRoute(ips);
        }
        /// <summary>
        /// 删除路由
        /// </summary>
        /// <param name="ips"></param>
        public void RemoveRoute(LinkerTunDeviceRouteItem[] ips)
        {
            linkerTunDevice?.RemoveRoute(ips);
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
        public bool Write(ReadOnlyMemory<byte> buffer)
        {
            if (linkerTunDevice != null && Status == LinkerTunDeviceStatus.Running)
            {
                MapToRealIP(buffer);
                return linkerTunDevice.Write(buffer);
            }
            return false;
        }

        private void ToMapIP(ReadOnlyMemory<byte> buffer)
        {
            //只支持映射IPV4
            if ((byte)(buffer.Span[0] >> 4 & 0b1111) != 4) return;
            //映射表不为空
            if (natDic.IsEmpty) return;

            //源IP
            uint realDist = NetworkHelper.ToValue(buffer.Span.Slice(12, 4));
            if (natDic.TryGetValue(realDist, out uint fakeDist))
            {
                //修改源IP
                ReWriteIP(buffer, fakeDist, 12);
            }
        }
        private void MapToRealIP(ReadOnlyMemory<byte> buffer)
        {
            //只支持映射IPV4
            if ((byte)(buffer.Span[0] >> 4 & 0b1111) != 4) return;
            //映射表不为空
            if (masks.Length == 0 || mapDic.Count == 0) return;
            //广播包
            if (buffer.Span[19] == 255) return;

            uint fakeDist = NetworkHelper.ToValue(buffer.Span.Slice(16, 4));
            for (int i = 0; i < masks.Length; i++)
            {
                //目标IP网络号存在映射表中，找到映射后的真实网络号，替换网络号得到最终真实的IP
                if (mapDic.TryGetValue(fakeDist & masks[i], out uint realNetwork))
                {
                    uint realDist = realNetwork | (fakeDist & ~masks[i]);
                    //修改目标IP
                    ReWriteIP(buffer, realDist, 16, linkerTunDevice.AppNat == false);
                    natDic.AddOrUpdate(realDist, fakeDist, (a, b) => fakeDist);
                    break;
                }
            }
        }
        /// <summary>
        /// 写入新IP
        /// </summary>
        /// <param name="packet">IP包</param>
        /// <param name="newIP">大端IP</param>
        /// <param name="pos">写入位置，源12，目的16</param>
        /// <param name="checksum">是否计算校验和，当windows使用应用层NAT后，会计算一次，这样可以减少一次计算</param>
        private unsafe void ReWriteIP(ReadOnlyMemory<byte> packet, uint newIP, int pos, bool checksum = true)
        {
            fixed (byte* ptr = packet.Span)
            {
                //修改目标IP，需要小端写入，IP计算都是按大端的，操作是小端的，所以转换一下
                *(uint*)(ptr + pos) = BinaryPrimitives.ReverseEndianness(newIP);
                if (checksum)
                {
                    //计算校验和
                    ChecksumHelper.Checksum(ptr, packet.Length);
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

        public async Task<bool> CheckAvailable(bool order = false)
        {
            return await linkerTunDevice.CheckAvailable(order);
        }
    }

    /// <summary>
    /// 映射对象
    /// </summary>
    public sealed class LanMapInfo
    {
        /// <summary>
        /// 假IP
        /// </summary>
        public IPAddress IP { get; set; }
        /// <summary>
        /// 真实IP
        /// </summary>
        public IPAddress ToIP { get; set; }
        /// <summary>
        /// 前缀
        /// </summary>
        public byte PrefixLength { get; set; }
    }
}
