
using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

#pragma warning disable CS1591

namespace linker.nat
{
    /// <summary>
    /// WinDivert 包装
    /// </summary>
    public class WinDivert : IDisposable
    {
        private readonly SafeWinDivertHandle handle;

        public const short PriorityHighest = 30000;
        public const short PriorityLowest = -PriorityHighest;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="filter">过滤器，文档https://reqrypt.org/windivert-doc.html</param>
        /// <param name="layer">网络层</param>
        /// <param name="priority">优先级 -15到15，默认0</param>
        /// <param name="flags">标志</param>
        public WinDivert(string filter, Layer layer, short priority, Flag flags)
        {
            var fobj = CompileFilter(filter, layer);
            handle = Open(fobj.Span, layer, priority, flags);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="filter">过滤器，文档https://reqrypt.org/windivert-doc.html</param>
        /// <param name="layer">网络层</param>
        /// <param name="priority">优先级 -15到15，默认0</param>
        /// <param name="flags">标志</param>
        public WinDivert(ReadOnlySpan<byte> filter, Layer layer, short priority, Flag flags) => handle = Open(filter, layer, priority, flags);

        private static unsafe SafeWinDivertHandle Open(ReadOnlySpan<byte> filter, Layer layer, short priority, Flag flags)
        {
            if (filter.IsEmpty) throw new ArgumentException($"{nameof(filter)} is empty.", nameof(filter));

            var hraw = (IntPtr)(-1);
            fixed (byte* pFilter = filter) hraw = NativeMethods.WinDivertOpen(pFilter, layer, priority, flags);
            if (hraw == (IntPtr)(-1)) throw new WinDivertException(nameof(NativeMethods.WinDivertOpen));
            return new SafeWinDivertHandle(hraw, true);
        }

        public const int BatchMax = 0xFF;
        public const int MTUMax = 40 + 0xFFFF;

        /// <summary>
        /// 接收数据包
        /// </summary>
        /// <param name="packet">包数量</param>
        /// <param name="abuf">地址数量</param>
        /// <returns></returns>
        /// <exception cref="WinDivertException"></exception>
        public unsafe (uint recvLen, uint addrLen) RecvEx(Span<byte> packet, Span<WinDivertAddress> abuf)
        {
            var recvLen = (uint)0;
            var addrLen = (uint)0;
            var pAddrLen = (uint*)null;
            if (!abuf.IsEmpty)
            {
                addrLen = (uint)(abuf.Length * sizeof(WinDivertAddress));
                pAddrLen = &addrLen;
            }

            using (var href = new SafeHandleReference(handle, (IntPtr)(-1)))
            {
                var success = false;
                fixed (byte* pPacket = packet) fixed (WinDivertAddress* pAbuf = abuf)
                {
                    success = NativeMethods.WinDivertRecvEx(href.RawHandle, pPacket, (uint)packet.Length, &recvLen, 0, pAbuf, pAddrLen, null);
                }
                if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertRecvEx));
            }

            addrLen /= (uint)sizeof(WinDivertAddress);
            return (recvLen, addrLen);
        }

        /// <summary>
        /// 注入包
        /// </summary>
        /// <param name="packet">TCP/IP包</param>
        /// <param name="addr">一个地址描述，要以什么样的方式发送到哪里</param>
        /// <returns></returns>
        /// <exception cref="WinDivertException"></exception>
        public unsafe uint SendEx(ReadOnlySpan<byte> packet, ReadOnlySpan<WinDivertAddress> addr)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var sendLen = (uint)0;
            var success = false;
            fixed (byte* pPacket = packet) fixed (WinDivertAddress* pAddr = addr)
            {
                success = NativeMethods.WinDivertSendEx(href.RawHandle, pPacket, (uint)packet.Length, &sendLen, 0, pAddr, (uint)(addr.Length * sizeof(WinDivertAddress)), null);
            }
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertSendEx));
            return sendLen;
        }

        public const ulong QueueLengthDefault = 4096;
        public const ulong QueueLengthMin = 32;
        public const ulong QueueLengthMax = 16384;
        public ulong QueueLength
        {
            get => GetParam(Param.QueueLength);
            set => SetParam(Param.QueueLength, value);
        }

        public const ulong QueueTimeDefault = 2000;
        public const ulong QueueTimeMin = 100;
        public const ulong QueueTimeMax = 16000;

        public ulong QueueTime
        {
            get => GetParam(Param.QueueTime);
            set => SetParam(Param.QueueTime, value);
        }

        public const ulong QueueSizeDefault = 4194304;
        public const ulong QueueSizeMin = 65535;
        public const ulong QueueSizeMax = 33554432;

        public ulong QueueSize
        {
            get => GetParam(Param.QueueSize);
            set => SetParam(Param.QueueSize, value);
        }

        public ulong VersionMajor => GetParam(Param.VersionMajor);

        public ulong VersionMinor => GetParam(Param.VersionMinor);

        private ulong GetParam(Param param)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var success = NativeMethods.WinDivertGetParam(href.RawHandle, param, out var value);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertGetParam));
            return value;
        }

        private void SetParam(Param param, ulong value)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var success = NativeMethods.WinDivertSetParam(href.RawHandle, param, value);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertSetParam));
        }

        /// <summary>
        /// 停止接收数据包
        /// </summary>
        public void ShutdownRecv() => Shutdown(ShutdownHow.Recv);

        /// <summary>
        /// 停止发送数据包
        /// </summary>
        public void ShutdownSend() => Shutdown(ShutdownHow.Send);

        /// <summary>
        /// 停止接收和发送
        /// </summary>
        public void Shutdown() => Shutdown(ShutdownHow.Both);

        private void Shutdown(ShutdownHow how)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var success = NativeMethods.WinDivertShutdown(href.RawHandle, how);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertShutdown));
        }

        /// <summary>
        /// 注销驱动
        /// </summary>
#pragma warning disable CA1816
        public void Dispose() => handle?.Dispose();
#pragma warning restore CA1816

        /// <summary>
        /// 计算校验和
        /// </summary>
        /// <param name="packet">数据包</param>
        /// <param name="addr">跟发送那个一样</param>
        /// <param name="flags">一些选择标志</param>
        /// <exception cref="ArgumentException"></exception>
        public static unsafe void CalcChecksums(ReadOnlySpan<byte> packet, ref WinDivertAddress addr, ChecksumFlag flags)
        {
            var success = false;
            fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = &addr)
            {
                success = NativeMethods.WinDivertHelperCalcChecksums(pPacket, (uint)packet.Length, pAddr, flags);
            }
            if (!success) throw new ArgumentException("An error occurred while calculating the checksum of the packet.");
        }

        /// <summary>
        /// 格式化过滤器
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        /// <exception cref="WinDivertInvalidFilterException"></exception>
        public static unsafe ReadOnlyMemory<byte> CompileFilter(string filter, Layer layer)
        {
            var buffer = (Span<byte>)stackalloc byte[256 * 24];
            var pErrorStr = (byte*)null;
            var errorPos = (uint)0;
            var success = false;

            fixed (byte* pBuffer = buffer) success = NativeMethods.WinDivertHelperCompileFilter(filter, layer, pBuffer, (uint)buffer.Length, &pErrorStr, &errorPos);
            if (!success)
            {
                var errorLen = 0;
                while (*(pErrorStr + errorLen) != 0) errorLen++;
                var errorStr = Encoding.ASCII.GetString(pErrorStr, errorLen);
                throw new WinDivertInvalidFilterException(errorStr, errorPos, nameof(filter));
            }

            var fobjLen = buffer.IndexOf((byte)0) + 1;
            var fobj = new Memory<byte>(new byte[fobjLen]);
            buffer[..fobjLen].CopyTo(fobj.Span);
            return fobj;
        }

        /// <summary>
        /// 格式化过滤器
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        /// <exception cref="WinDivertException"></exception>
        public static unsafe string FormatFilter(ReadOnlySpan<byte> filter, Layer layer)
        {
            var buffer = (Span<byte>)stackalloc byte[30000];
            var success = false;

            fixed (byte* pFilter = filter) fixed (byte* pBuffer = buffer)
            {
                success = NativeMethods.WinDivertHelperFormatFilter(pFilter, layer, pBuffer, (uint)buffer.Length);
            }
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertHelperFormatFilter));

            var strlen = buffer.IndexOf((byte)0);
            return Encoding.ASCII.GetString(buffer[..strlen]);
        }

        public enum Layer
        {
            /// <summary>
            /// 网络层
            /// </summary>
            Network = 0,

            /// <summary>
            /// 网络转发层
            /// </summary>
            NetworkForward = 1,

            /// <summary>
            /// 流层，TCP/UDP数据包
            /// </summary>
            Flow = 2,

            /// <summary>
            /// socket层，bind，connect什么的
            /// </summary>
            Socket = 3,

            /// <summary>
            /// WinDivert 自身发送的包
            /// </summary>
            Reflect = 4,
        }

        public enum Event
        {
            /// <summary>
            /// 收到一个包
            /// </summary>
            NetworkPacket = 0,

            /// <summary>
            /// TCP/UDP开始通信
            /// </summary>
            FlowEstablished = 1,

            /// <summary>
            /// TCP/UDP结束通信
            /// </summary>
            FlowDeleted = 2,

            /// <summary>
            /// bind
            /// </summary>
            SocketBind = 3,

            /// <summary>
            /// connect
            /// </summary>
            SocketConnect = 4,

            /// <summary>
            /// listen
            /// </summary>
            SocketListen = 5,

            /// <summary>
            /// accept
            /// </summary>
            SocketAccept = 6,

            /// <summary>
            /// close
            /// </summary>
            SocketClose = 7,

            /// <summary>
            /// 开始驱动
            /// </summary>
            ReflectOpen = 8,

            /// <summary>
            /// 结束驱动
            /// </summary>
            ReflectClose = 9,
        }

        [Flags]
        public enum Flag : ulong
        {
            /// <summary>
            /// 嗅探，不阻止，适合抓包
            /// </summary>
            Sniff = 0x0001,

            /// <summary>
            /// 捕获到直接丢弃
            /// </summary>
            Drop = 0x0002,

            /// <summary>
            /// 只接收
            /// </summary>
            RecvOnly = 0x0004,

            /// <summary>
            /// 只接收
            /// </summary>
            ReadOnly = RecvOnly,

            /// <summary>
            /// 只发送
            /// </summary>
            SendOnly = 0x0008,

            /// <summary>
            /// 只接收
            /// </summary>
            WriteOnly = SendOnly,


            NoInstall = 0x0010,

            /// <summary>
            /// 捕获分片
            /// </summary>
            Fragments = 0x0020,
        }

        internal enum Param
        {
            QueueLength = 0,
            QueueTime = 1,
            QueueSize = 2,
            VersionMajor = 3,
            VersionMinor = 4,
        }

        internal enum ShutdownHow
        {
            Recv = 0x1,
            Send = 0x2,
            Both = 0x3,
        }

        [Flags]
        public enum ChecksumFlag : ulong
        {
            /// <summary>
            /// 不算ipv4
            /// </summary>
            NoIPv4Checksum = 1,

            /// <summary>
            /// 不算ipv4 的icmp
            /// </summary>
            NoICMPv4Checksum = 2,

            /// <summary>
            /// 不算ipv6 的icmp
            /// </summary>
            NoICMPv6Checksum = 4,

            /// <summary>
            /// 不算TCP
            /// </summary>
            NoTCPChecksum = 8,

            /// <summary>
            /// 不算UDP
            /// </summary>
            NoUDPChecksum = 16,
        }
    }

    internal sealed class SafeWinDivertHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeWinDivertHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle) => SetHandle(existingHandle);
        protected override bool ReleaseHandle() => NativeMethods.WinDivertClose(handle);
    }

    /// <summary>
    /// 安全句柄
    /// </summary>
    public struct SafeHandleReference : IDisposable
    {
        public IntPtr RawHandle { get; private set; }

        private readonly SafeHandle handle;
        private readonly IntPtr invalid;
        private bool reference;

        public SafeHandleReference(SafeHandle? handle, IntPtr invalid)
        {
            RawHandle = invalid;
            this.handle = handle;
            this.invalid = invalid;
            reference = false;
            if (handle is null || handle.IsInvalid || handle.IsClosed) return;
            handle.DangerousAddRef(ref reference);
            RawHandle = handle.DangerousGetHandle();
        }

        public void Dispose()
        {
            RawHandle = invalid;
            if (reference)
            {
                handle?.DangerousRelease();
                reference = false;
            }
        }
    }

    /// <summary>
    /// 异常
    /// </summary>
    public class WinDivertException : Win32Exception
    {
        public string WinDivertNativeMethod { get; }

        internal WinDivertException(string method) : base() => WinDivertNativeMethod = method;
    }

    /// <summary>
    /// 解析数据包，然后拿去 foreach (var (i, p) in new WinDivertIndexedPacketParser(packet))，其实调用了WinDivertPacketParser
    /// </summary>
    public struct WinDivertIndexedPacketParser : IEnumerable<(int, WinDivertParseResult)>
    {
        private readonly WinDivertPacketParser e;

        public WinDivertIndexedPacketParser(ReadOnlyMemory<byte> packet) => e = new WinDivertPacketParser(packet);

        public WinDivertIndexedPacketParser(WinDivertPacketParser e) => this.e = e;

        public WinDivertIndexedPacketEnumerator GetEnumerator() => new(e.GetEnumerator());

        IEnumerator<(int, WinDivertParseResult)> IEnumerable<(int, WinDivertParseResult)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public struct WinDivertIndexedPacketEnumerator : IEnumerator<(int, WinDivertParseResult)>
    {
        private WinDivertPacketEnumerator e;
        private int i;

        public (int, WinDivertParseResult) Current => (i, e.Current);
        object IEnumerator.Current => Current;

        internal WinDivertIndexedPacketEnumerator(WinDivertPacketEnumerator e)
        {
            this.e = e;
            i = -1;
        }

        public bool MoveNext()
        {
            var success = e.MoveNext();
            if (!success) return false;
            i++;
            return true;
        }

        public void Reset()
        {
            e.Reset();
            i = -1;
        }

        public void Dispose() => e.Dispose();
    }

    public struct WinDivertPacketParser : IEnumerable<WinDivertParseResult>
    {
        private readonly ReadOnlyMemory<byte> packet;
        public WinDivertPacketParser(ReadOnlyMemory<byte> packet) => this.packet = packet;
        public WinDivertPacketEnumerator GetEnumerator() => new(packet);

        IEnumerator<WinDivertParseResult> IEnumerable<WinDivertParseResult>.GetEnumerator() => new WinDivertPacketEnumerator(packet);
        IEnumerator IEnumerable.GetEnumerator() => new WinDivertPacketEnumerator(packet);
    }

    public unsafe struct WinDivertPacketEnumerator : IEnumerator<WinDivertParseResult>
    {
        private readonly MemoryHandle hmem;
        private readonly ReadOnlyMemory<byte> packet;
        private readonly byte* pPacket0;
        private byte* pPacket;
        private uint packetLen;

        private WinDivertParseResult current;
        public WinDivertParseResult Current => current;
        object IEnumerator.Current => current;

        internal WinDivertPacketEnumerator(ReadOnlyMemory<byte> packet)
        {
            hmem = packet.Pin();
            this.packet = packet;
            pPacket0 = (byte*)hmem.Pointer;
            pPacket = pPacket0;
            packetLen = (uint)packet.Length;
            current = new WinDivertParseResult();
        }

        public bool MoveNext()
        {
            var ipv4Hdr = (WinDivertIPv4Hdr*)null;
            var ipv6Hdr = (WinDivertIPv6Hdr*)null;
            var protocol = (byte)0;
            var icmpv4Hdr = (WinDivertICMPv4Hdr*)null;
            var icmpv6Hdr = (WinDivertICMPv6Hdr*)null;
            var tcpHdr = (WinDivertTCPHdr*)null;
            var udpHdr = (WinDivertUDPHdr*)null;
            var pData = (byte*)null;
            var dataLen = (uint)0;
            var pNext = (byte*)null;
            var nextLen = (uint)0;

            var success = NativeMethods.WinDivertHelperParsePacket(pPacket, packetLen, &ipv4Hdr, &ipv6Hdr, &protocol, &icmpv4Hdr, &icmpv6Hdr, &tcpHdr, &udpHdr, (void**)&pData, &dataLen, (void**)&pNext, &nextLen);
            if (!success) return false;

            current.Packet = pNext != null
                ? packet[(int)(pPacket - pPacket0)..(int)(pNext - pPacket0)]
                : packet[(int)(pPacket - pPacket0)..(int)(pPacket + packetLen - pPacket0)];
            current.IPv4Hdr = ipv4Hdr;
            current.IPv6Hdr = ipv6Hdr;
            current.Protocol = protocol;
            current.ICMPv4Hdr = icmpv4Hdr;
            current.ICMPv6Hdr = icmpv6Hdr;
            current.TCPHdr = tcpHdr;
            current.UDPHdr = udpHdr;
            current.Data = pData != null && dataLen > 0
                ? packet[(int)(pData - pPacket0)..(int)(pData + dataLen - pPacket0)]
                : ReadOnlyMemory<byte>.Empty;

            pPacket = pNext;
            packetLen = nextLen;
            return true;
        }

        public void Reset()
        {
            pPacket = pPacket0;
            packetLen = (uint)packet.Length;
            current = new WinDivertParseResult();
        }

        public void Dispose() => hmem.Dispose();
    }

    /// <summary>
    /// 解析数据包后的结果
    /// </summary>
    public unsafe struct WinDivertParseResult
    {
        /// <summary>
        /// 原始TCP/IP包
        /// </summary>
        public ReadOnlyMemory<byte> Packet;
        /// <summary>
        /// ipv4头
        /// </summary>
        public WinDivertIPv4Hdr* IPv4Hdr;
        /// <summary>
        /// ipv6头
        /// </summary>
        public WinDivertIPv6Hdr* IPv6Hdr;
        /// <summary>
        /// 协议
        /// </summary>
        public byte Protocol;
        /// <summary>
        /// ipv4 icmp头
        /// </summary>
        public WinDivertICMPv4Hdr* ICMPv4Hdr;

        /// <summary>
        /// ipv6 icmp头
        /// </summary>
        public WinDivertICMPv6Hdr* ICMPv6Hdr;

        /// <summary>
        /// tcp头
        /// </summary>
        public WinDivertTCPHdr* TCPHdr;

        /// <summary>
        /// udp头
        /// </summary>
        public WinDivertUDPHdr* UDPHdr;

        /// <summary>
        /// 负载数据
        /// </summary>
        public ReadOnlyMemory<byte> Data;
    }

    public class WinDivertInvalidFilterException : ArgumentException
    {
        public string FilterErrorStr;

        public uint FilterErrorPos;

        public WinDivertInvalidFilterException(string errorStr, uint errorPos, string? paramName) : base(errorStr, paramName)
        {
            FilterErrorStr = errorStr;
            FilterErrorPos = errorPos;
        }
    }

    /// <summary>
    /// ipv4地址 本机序
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct IPv4Addr : IEquatable<IPv4Addr>
    {
        internal uint Raw;

        public static unsafe IPv4Addr Parse(string addrStr)
        {
            var addr = new IPv4Addr();
            var success = NativeMethods.WinDivertHelperParseIPv4Address(addrStr, &addr.Raw);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertHelperParseIPv4Address));
            return addr;
        }

        public override unsafe string ToString()
        {
            var buffer = (Span<byte>)stackalloc byte[32];
            var success = false;
            fixed (byte* pBuffer = buffer) success = NativeMethods.WinDivertHelperFormatIPv4Address(Raw, pBuffer, (uint)buffer.Length);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertHelperFormatIPv4Address));

            var strlen = buffer.IndexOf((byte)0);
            return Encoding.ASCII.GetString(buffer[..strlen]);
        }

        public static bool operator ==(IPv4Addr left, IPv4Addr right) => left.Equals(right);
        public static bool operator !=(IPv4Addr left, IPv4Addr right) => !left.Equals(right);

        public bool Equals(IPv4Addr addr) => Raw == addr.Raw;

        public override bool Equals(object? obj)
        {
            if (obj is IPv4Addr ipv4Addr) return Equals(ipv4Addr);
            return base.Equals(obj);
        }

        public override int GetHashCode() => HashCode.Combine(Raw);
    }

    /// <summary>
    /// ipv4地址 网络序
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NetworkIPv4Addr : IEquatable<NetworkIPv4Addr>
    {
        internal uint Raw;

        public override string ToString() => ((IPv4Addr)this).ToString();

        public static bool operator ==(NetworkIPv4Addr left, NetworkIPv4Addr right) => left.Equals(right);
        public static bool operator !=(NetworkIPv4Addr left, NetworkIPv4Addr right) => !left.Equals(right);

        public bool Equals(NetworkIPv4Addr addr) => Raw == addr.Raw;

        public static implicit operator NetworkIPv4Addr(IPv4Addr addr) => new()
        {
            Raw = NativeMethods.WinDivertHelperHtonl(addr.Raw),
        };

        public static implicit operator IPv4Addr(NetworkIPv4Addr addr) => new()
        {
            Raw = NativeMethods.WinDivertHelperNtohl(addr.Raw),
        };

        public override bool Equals(object? obj)
        {
            if (obj is NetworkIPv4Addr addr) return Equals(addr);
            return base.Equals(obj);
        }

        public override int GetHashCode() => HashCode.Combine(Raw);
    }

    /// <summary>
    /// ipv6 主机序
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IPv6Addr : IEquatable<IPv6Addr>
    {
        internal fixed uint Raw[4];

        public static IPv6Addr Parse(string addrStr)
        {
            var addr = new IPv6Addr();
            var success = NativeMethods.WinDivertHelperParseIPv6Address(addrStr, addr.Raw);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertHelperParseIPv6Address));
            return addr;
        }
        public override string ToString()
        {
            var buffer = (Span<byte>)stackalloc byte[64];
            var success = false;
            fixed (uint* addr = Raw) fixed (byte* pBuffer = buffer)
            {
                success = NativeMethods.WinDivertHelperFormatIPv6Address(addr, pBuffer, (uint)buffer.Length);
            }
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertHelperFormatIPv6Address));

            var strlen = buffer.IndexOf((byte)0);
            return Encoding.ASCII.GetString(buffer[..strlen]);
        }

        public static bool operator ==(IPv6Addr left, IPv6Addr right) => left.Equals(right);
        public static bool operator !=(IPv6Addr left, IPv6Addr right) => !left.Equals(right);

        public bool Equals(IPv6Addr addr)
        {
            return Raw[0] == addr.Raw[0]
                && Raw[1] == addr.Raw[1]
                && Raw[2] == addr.Raw[2]
                && Raw[3] == addr.Raw[3];
        }

        public override bool Equals(object? obj)
        {
            if (obj is IPv6Addr ipv6Addr) return Equals(ipv6Addr);
            return base.Equals(obj);
        }

        public override unsafe int GetHashCode() => HashCode.Combine(Raw[0], Raw[1], Raw[2], Raw[3]);
    }

    /// <summary>
    /// ipv6 网络序
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NetworkIPv6Addr : IEquatable<NetworkIPv6Addr>
    {
        internal fixed uint Raw[4];

        public override string ToString() => ((IPv6Addr)this).ToString();

        public static bool operator ==(NetworkIPv6Addr left, NetworkIPv6Addr right) => left.Equals(right);
        public static bool operator !=(NetworkIPv6Addr left, NetworkIPv6Addr right) => !left.Equals(right);

        public bool Equals(NetworkIPv6Addr addr)
        {
            return Raw[0] == addr.Raw[0]
                && Raw[1] == addr.Raw[1]
                && Raw[2] == addr.Raw[2]
                && Raw[3] == addr.Raw[3];
        }

        public static implicit operator NetworkIPv6Addr(IPv6Addr addr)
        {
            var naddr = new NetworkIPv6Addr();
            NativeMethods.WinDivertHelperHtonIPv6Address(addr.Raw, naddr.Raw);
            return naddr;
        }

        public static implicit operator IPv6Addr(NetworkIPv6Addr addr)
        {
            var haddr = new IPv6Addr();
            NativeMethods.WinDivertHelperNtohIPv6Address(addr.Raw, haddr.Raw);
            return haddr;
        }

        public override bool Equals(object? obj)
        {
            if (obj is NetworkIPv6Addr addr) return Equals(addr);
            return base.Equals(addr);
        }

        public override unsafe int GetHashCode() => HashCode.Combine(Raw[0], Raw[1], Raw[2], Raw[3]);
    }

    /// <summary>
    /// u16 网络序
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NetworkUInt16 : IEquatable<NetworkUInt16>
    {
        private readonly ushort raw;

        private NetworkUInt16(ushort raw) => this.raw = raw;
        public static implicit operator NetworkUInt16(ushort x) => new(NativeMethods.WinDivertHelperHtons(x));
        public static implicit operator ushort(NetworkUInt16 x) => NativeMethods.WinDivertHelperNtohs(x.raw);
        public static bool operator ==(NetworkUInt16 left, NetworkUInt16 right) => left.Equals(right);
        public static bool operator !=(NetworkUInt16 left, NetworkUInt16 right) => !left.Equals(right);
        public bool Equals(NetworkUInt16 x) => raw == x.raw;

        public override bool Equals(object? obj)
        {
            if (obj is NetworkUInt16 x) return Equals(x);
            return base.Equals(obj);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => ((ushort)this).ToString();
    }

    /// <summary>
    /// u32 网络序
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NetworkUInt32 : IEquatable<NetworkUInt32>
    {
        private readonly uint raw;

        private NetworkUInt32(uint raw) => this.raw = raw;
        public static implicit operator NetworkUInt32(uint x) => new(NativeMethods.WinDivertHelperHtonl(x));
        public static implicit operator uint(NetworkUInt32 x) => NativeMethods.WinDivertHelperNtohl(x.raw);
        public static bool operator ==(NetworkUInt32 left, NetworkUInt32 right) => left.Equals(right);
        public static bool operator !=(NetworkUInt32 left, NetworkUInt32 right) => !left.Equals(right);
        public bool Equals(NetworkUInt32 x) => raw == x.raw;

        public override bool Equals(object? obj)
        {
            if (obj is NetworkUInt32 x) return Equals(x);
            return base.Equals(obj);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => ((uint)this).ToString();
    }

#pragma warning disable CA2101
    internal static class NativeMethods
    {
        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe IntPtr WinDivertOpen(byte* filter, WinDivert.Layer layer, short priority, WinDivert.Flag flags);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertRecvEx(IntPtr handle, void* packet, uint packetLen, uint* recvLen, ulong flags, WinDivertAddress* addr, uint* addrLen, NativeOverlapped* overlapped);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertSendEx(IntPtr handle, void* packet, uint packetLen, uint* sendLen, ulong flags, WinDivertAddress* addr, uint addrLen, NativeOverlapped* overlapped);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertSetParam(IntPtr handle, WinDivert.Param param, ulong value);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertGetParam(IntPtr handle, WinDivert.Param param, out ulong value);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertShutdown(IntPtr handle, WinDivert.ShutdownHow how);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertClose(IntPtr handle);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern unsafe bool WinDivertHelperParsePacket(void* packet, uint packetLen, WinDivertIPv4Hdr** ipv4Hdr, WinDivertIPv6Hdr** ipv6Hdr, byte* protocol, WinDivertICMPv4Hdr** icmpv4Hdr, WinDivertICMPv6Hdr** icmpv6Hdr, WinDivertTCPHdr** tcpHdr, WinDivertUDPHdr** udpHdr, void** data, uint* dataLen, void** next, uint* nextLen);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperParseIPv4Address(string addrStr, uint* addr);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperParseIPv6Address(string addrStr, uint* addr);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperFormatIPv4Address(uint addr, byte* buffer, uint buflen);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperFormatIPv6Address(uint* addr, byte* buffer, uint buflen);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern unsafe bool WinDivertHelperCalcChecksums(void* packet, uint packetLen, WinDivertAddress* addr, WinDivert.ChecksumFlag flags);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern unsafe bool WinDivertHelperCompileFilter(string filter, WinDivert.Layer layer, byte* fobj, uint fobjLen, byte** errorStr, uint* errorPos);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperFormatFilter(byte* filter, WinDivert.Layer layer, byte* buffer, uint bufLen);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern ushort WinDivertHelperNtohs(ushort x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern ushort WinDivertHelperHtons(ushort x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern uint WinDivertHelperNtohl(uint x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern uint WinDivertHelperHtonl(uint x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern unsafe void WinDivertHelperNtohIPv6Address(uint* inAddr, uint* outAddr);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = false)]
        public static extern unsafe void WinDivertHelperHtonIPv6Address(uint* inAddr, uint* outAddr);
    }
#pragma warning restore CA2101

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct WinDivertAddress
    {
        [FieldOffset(0)]
        public long Timestamp;

        [FieldOffset(8)] private byte byteLayer;
        [FieldOffset(9)] private byte byteEvent;
        [FieldOffset(10)] private byte flags;

        [FieldOffset(16)]
        public WinDivertDataNetwork Network;
        [FieldOffset(16)]
        public WinDivertDataFlow Flow;
        [FieldOffset(16)]
        public WinDivertDataSocket Socket;
        [FieldOffset(16)]
        public WinDivertDataReflect Reflect;

        [FieldOffset(16)] private fixed byte reserved[64];

        public WinDivert.Layer Layer
        {
            get => (WinDivert.Layer)byteLayer;
            set => byteLayer = (byte)value;
        }
        public WinDivert.Event Event
        {
            get => (WinDivert.Event)byteEvent;
            set => byteEvent = (byte)value;
        }

        public bool Sniffed
        {
            get => GetFlag(1 << 0);
            set => SetFlag(1 << 0, value);
        }

        public bool Outbound
        {
            get => GetFlag(1 << 1);
            set => SetFlag(1 << 1, value);
        }

        public bool Loopback
        {
            get => GetFlag(1 << 2);
            set => SetFlag(1 << 2, value);
        }

        public bool Impostor
        {
            get => GetFlag(1 << 3);
            set => SetFlag(1 << 3, value);
        }

        public bool IPv6
        {
            get => GetFlag(1 << 4);
            set => SetFlag(1 << 4, value);
        }

        public bool IPChecksum
        {
            get => GetFlag(1 << 5);
            set => SetFlag(1 << 5, value);
        }

        public bool TCPChecksum
        {
            get => GetFlag(1 << 6);
            set => SetFlag(1 << 6, value);
        }

        public bool UDPChecksum
        {
            get => GetFlag(1 << 7);
            set => SetFlag(1 << 7, value);
        }

        private bool GetFlag(byte bit) => (flags & bit) != 0;

        private void SetFlag(byte bit, bool val)
        {
            if (val) flags = (byte)(flags | bit);
            else flags = (byte)((flags | bit) ^ bit);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertDataNetwork
    {
        public uint IfIdx;
        public uint SubIfIdx;
    }

    /// <summary>
    /// TCP/UDP包
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertDataFlow
    {
        public ulong EndpointId;
        public ulong ParentEndpointId;
        public uint ProcessId;
        public IPv6Addr LocalAddr;
        public IPv6Addr RemoteAddr;
        public ushort LocalPort;
        public ushort RemotePort;
        public byte Protocol;
    }

    /// <summary>
    /// socket包
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertDataSocket
    {
        public ulong EndpointId;
        public ulong ParentEndpointId;
        public uint ProcessId;
        public IPv6Addr LocalAddr;
        public IPv6Addr RemoteAddr;
        public ushort LocalPort;
        public ushort RemotePort;
        public byte Protocol;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertDataReflect
    {
        /// <summary>
        /// A timestamp indicating when the handle was opened.
        /// </summary>
        public long Timestamp;

        /// <summary>
        /// The ID of the process that opened the handle.
        /// </summary>
        public uint ProcessId;

        /// <summary>
        /// The <c>layer</c> parameter passed to <c>WinDivertOpen()</c> for the opened handle.
        /// </summary>
        public WinDivert.Layer Layer;

        /// <summary>
        /// The <c>flags</c> parameter passed to <c>WinDivertOpen()</c> for the opened handle.
        /// </summary>
        public ulong Flags;

        /// <summary>
        /// The <c>priority</c> parameter passed to <c>WinDivertOpen()</c> for the opened handle.
        /// </summary>
        public short Priority;
    }

    /// <summary>
    /// IPV4 头
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertIPv4Hdr
    {
        public byte HdrLengthOff0;
        public byte TOS;
        public NetworkUInt16 Length;
        public NetworkUInt16 Id;
        public ushort FragOff0;
        public byte TTL;
        public byte Protocol;
        public ushort Checksum;
        public NetworkIPv4Addr SrcAddr;
        public NetworkIPv4Addr DstAddr;
    }

    /// <summary>
    /// IPV6 头
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertIPv6Hdr
    {
        public uint FlowLabelOff0;
        public NetworkUInt16 Length;
        public byte NextHdr;
        public byte HopLimit;
        public NetworkIPv6Addr SrcAddr;
        public NetworkIPv6Addr DstAddr;
    }

    /// <summary>
    /// ICMP V4头
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertICMPv4Hdr
    {
        public byte Type;
        public byte Code;
        public ushort Checksum;
        public uint Body;
    }

    /// <summary>
    /// ICMP V6头
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertICMPv6Hdr
    {
        public byte Type;
        public byte Code;
        public ushort Checksum;
        public uint Body;
    }
    /// <summary>
    /// TCP头
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertTCPHdr
    {
        public NetworkUInt16 SrcPort;
        public NetworkUInt16 DstPort;
        public NetworkUInt32 SeqNum;
        public NetworkUInt32 AckNum;
        public ushort FinOff0;
        public NetworkUInt16 Window;
        public ushort Checksum;
        public NetworkUInt16 UrgPtr;
    }

    /// <summary>
    /// UDP 头
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertUDPHdr
    {
        public NetworkUInt16 SrcPort;
        public NetworkUInt16 DstPort;
        public NetworkUInt16 Length;
        public ushort Checksum;
    }
}