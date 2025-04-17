/*
* SharpDivert.cs
* Copyright gcrtnst
*
* This file is part of SharpDivert.
*
* SharpDivert is free software: you can redistribute it and/or modify it
* under the terms of the GNU Lesser General Public License as published by the
* Free Software Foundation, either version 3 of the License, or (at your
* option) any later version.
*
* SharpDivert is distributed in the hope that it will be useful, but
* WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
* or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
* License for more details.
*
* You should have received a copy of the GNU Lesser General Public License
* along with SharpDivert.  If not, see <http://www.gnu.org/licenses/>.
*
* SharpDivert is free software; you can redistribute it and/or modify it
* under the terms of the GNU General Public License as published by the Free
* Software Foundation; either version 2 of the License, or (at your option)
* any later version.
* 
* SharpDivert is distributed in the hope that it will be useful, but
* WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
* or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
* for more details.
* 
* You should have received a copy of the GNU General Public License along
* with SharpDivert; if not, write to the Free Software Foundation, Inc., 51
* Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
*/
using System.Buffers;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

#pragma warning disable CS1591

namespace linker.tun
{
    /// <summary>
    /// Allows user-mode applications to capture/modify/drop network packets sent to/from the Windows network stack.
    /// </summary>
    public class WinDivert : IDisposable
    {
        private readonly SafeWinDivertHandle handle;

        public const short PriorityHighest = 30000;
        public const short PriorityLowest = -PriorityHighest;

        /// <summary>
        /// Initializes a new instance of the <see cref="WinDivert"/> class.
        /// </summary>
        /// <param name="filter">A packet filter string specified in the WinDivert filter language.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="priority">The priority of the handle.</param>
        /// <param name="flags">Additional flags.</param>
        /// <exception cref="WinDivertInvalidFilterException">Thrown when the <paramref name="filter"/> is invalid.</exception>
        /// <exception cref="WinDivertException">Thrown when a WinDivert handle fails to open.</exception>
        public WinDivert(string filter, Layer layer, short priority, Flag flags)
        {
            var fobj = CompileFilter(filter, layer);
            handle = Open(fobj.Span, layer, priority, flags);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WinDivert"/> class with a compiled filter object.
        /// </summary>
        /// <param name="filter">A filter object compiled by <see cref="CompileFilter"/>. Passing non-null-terminated data may cause out-of-bounds access.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="priority">The priority of the handle.</param>
        /// <param name="flags">Additional flags.</param>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="filter"/> is empty.</exception>
        /// <exception cref="WinDivertException">Thrown when a WinDivert handle fails to open.</exception>
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
        /// Receives captured packets/events matching the filter passed to the constructor.
        /// </summary>
        /// <param name="packet">An buffer for the captured packet. Can be empty if packets are not needed.</param>
        /// <param name="abuf">An buffer for the address of the captured packet/event. Can be empty if addresses are not needed.</param>
        /// <returns><c>recvLen</c> is the total number of bytes written to <paramref name="packet"/>. <c>addrLen</c> is the total number of addresses written to <paramref name="abuf"/>.</returns>
        /// <exception cref="WinDivertException">Thrown when a packet fails to be received.</exception>
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
        /// Injects packets into the network stack.
        /// </summary>
        /// <param name="packet">A buffer containing a packet to be injected.</param>
        /// <param name="addr">The address of the injected packet.</param>
        /// <returns>The total number of bytes injected.</returns>
        /// <exception cref="WinDivertException">Throws when a packet fails to be injected.</exception>
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

        /// <summary>
        /// The maximum length of the packet queue for <see cref="RecvEx"/>.
        /// </summary>
        /// <remarks>
        /// Setting an out-of-range value will cause a <see cref="WinDivertException"/>.
        /// </remarks>
        public ulong QueueLength
        {
            get => GetParam(Param.QueueLength);
            set => SetParam(Param.QueueLength, value);
        }

        public const ulong QueueTimeDefault = 2000;
        public const ulong QueueTimeMin = 100;
        public const ulong QueueTimeMax = 16000;

        /// <summary>
        /// The minimum time, in milliseconds, a packet can be queued before it is automatically dropped.
        /// </summary>
        /// <remarks>
        /// Setting an out-of-range value will cause a <see cref="WinDivertException"/>.
        /// </remarks>
        public ulong QueueTime
        {
            get => GetParam(Param.QueueTime);
            set => SetParam(Param.QueueTime, value);
        }

        public const ulong QueueSizeDefault = 4194304;
        public const ulong QueueSizeMin = 65535;
        public const ulong QueueSizeMax = 33554432;

        /// <summary>
        /// The maximum number of bytes that can be stored in the packet queue for <see cref="RecvEx"/>.
        /// </summary>
        /// <remarks>
        /// Setting an out-of-range value will cause a <see cref="WinDivertException"/>.
        /// </remarks>
        public ulong QueueSize
        {
            get => GetParam(Param.QueueSize);
            set => SetParam(Param.QueueSize, value);
        }

        /// <summary>
        /// The major version of the WinDivert driver.
        /// </summary>
        public ulong VersionMajor => GetParam(Param.VersionMajor);

        /// <summary>
        /// The minor version of the WinDivert driver.
        /// </summary>
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
        /// Stop new packets being queued for <see cref="RecvEx"/>
        /// </summary>
        /// <exception cref="WinDivertException">Thrown when an error occurs.</exception>
        public void ShutdownRecv() => Shutdown(ShutdownHow.Recv);

        /// <summary>
        /// Stop new packets being injected via <see cref="SendEx"/>
        /// </summary>
        /// <exception cref="WinDivertException">Thrown when an error occurs.</exception>
        public void ShutdownSend() => Shutdown(ShutdownHow.Send);

        /// <summary>
        /// Causes all of a WinDivert handle to be shut down.
        /// </summary>
        /// <exception cref="WinDivertException">Thrown when an error occurs.</exception>
        public void Shutdown() => Shutdown(ShutdownHow.Both);

        private void Shutdown(ShutdownHow how)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var success = NativeMethods.WinDivertShutdown(href.RawHandle, how);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertShutdown));
        }

        /// <summary>
        /// Closes a WinDivert handle.
        /// </summary>
#pragma warning disable CA1816
        public void Dispose() => handle.Dispose();
#pragma warning restore CA1816

        /// <summary>
        /// (Re)calculates the checksum for any IPv4/ICMP/ICMPv6/TCP/UDP checksum present in the given packet.
        /// </summary>
        /// <param name="packet">The packet to be modified.</param>
        /// <param name="addr">The address.</param>
        /// <param name="flags">Additional flags.</param>
        /// <exception cref="ArgumentException"></exception>
        public static unsafe void CalcChecksums(Span<byte> packet, ref WinDivertAddress addr, ChecksumFlag flags)
        {
            var success = false;
            fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = &addr)
            {
                success = NativeMethods.WinDivertHelperCalcChecksums(pPacket, (uint)packet.Length, pAddr, flags);
            }
            if (!success) throw new ArgumentException("An error occurred while calculating the checksum of the packet.");
        }

        /// <summary>
        /// Compiles the given packet filter string into a compact object representation.
        /// </summary>
        /// <param name="filter">The packet filter string to be checked.</param>
        /// <param name="layer">The layer.</param>
        /// <returns>The compiled filter object.</returns>
        /// <exception cref="WinDivertInvalidFilterException">Thrown when the <paramref name="filter"/> is invalid.</exception>
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
        /// Formats the given filter string or object.
        /// </summary>
        /// <param name="filter">The packet filter string to be evaluated. Passing non-null-terminated data may cause out-of-bounds access.</param>
        /// <param name="layer">The layer.</param>
        /// <returns>The formatted filter.</returns>
        /// <exception cref="WinDivertException">Thrown when the <paramref name="filter"/> is invalid.</exception>
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
            /// Network packets to/from the local machine.
            /// </summary>
            Network = 0,

            /// <summary>
            /// Network packets passing through the local machine.
            /// </summary>
            NetworkForward = 1,

            /// <summary>
            /// Network flow established/deleted events.
            /// </summary>
            Flow = 2,

            /// <summary>
            /// Socket operation events.
            /// </summary>
            Socket = 3,

            /// <summary>
            /// WinDivert handle events.
            /// </summary>
            Reflect = 4,
        }

        public enum Event
        {
            /// <summary>
            /// A new network packet.
            /// </summary>
            NetworkPacket = 0,

            /// <summary>
            /// A new flow is created.
            /// </summary>
            FlowEstablished = 1,

            /// <summary>
            /// An old flow is deleted.
            /// </summary>
            FlowDeleted = 2,

            /// <summary>
            /// A <c>bind()</c> operation.
            /// </summary>
            SocketBind = 3,

            /// <summary>
            /// A <c>connect()</c> operation.
            /// </summary>
            SocketConnect = 4,

            /// <summary>
            /// A <c> listen()</c> operation.
            /// </summary>
            SocketListen = 5,

            /// <summary>
            /// An <c>accept()</c> operation.
            /// </summary>
            SocketAccept = 6,

            /// <summary>
            /// A socket endpoint is closed.
            /// </summary>
            SocketClose = 7,

            /// <summary>
            /// A new WinDivert handle was opened.
            /// </summary>
            ReflectOpen = 8,

            /// <summary>
            /// An old WinDivert handle was closed.
            /// </summary>
            ReflectClose = 9,
        }

        [Flags]
        public enum Flag : ulong
        {
            /// <summary>
            /// This flag opens the WinDivert handle in packet sniffing mode.
            /// In packet sniffing mode the original packet is not dropped-and-diverted (the default) but copied-and-diverted.
            /// </summary>
            Sniff = 0x0001,

            /// <summary>
            /// This flag indicates that the user application does not intend to read matching packets with <see cref="RecvEx"/>, instead the packets should be silently dropped.
            /// </summary>
            Drop = 0x0002,

            /// <summary>
            /// This flags forces the handle into receive only mode which effectively disables <see cref="SendEx"/>.
            /// This means that it is possible to block/capture packets or events but not inject them.
            /// </summary>
            RecvOnly = 0x0004,

            /// <summary>
            /// An alias for <see cref="RecvOnly"/>.
            /// </summary>
            ReadOnly = RecvOnly,

            /// <summary>
            /// This flags forces the handle into send only mode which effectively disables <see cref="RecvEx"/>.
            /// This means that it is possible to inject packets or events, but not block/capture them.
            /// </summary>
            SendOnly = 0x0008,

            /// <summary>
            /// An alias for <see cref="SendOnly"/>.
            /// </summary>
            WriteOnly = SendOnly,

            /// <summary>
            /// This flags causes the constructor to fail with <c>ERROR_SERVICE_DOES_NOT_EXIST</c> if the WinDivert driver is not already installed.
            /// </summary>
            NoInstall = 0x0010,

            /// <summary>
            /// If set, the handle will capture inbound IP fragments, but not inbound reassembled IP packets.
            /// Otherwise, if not set (the default), the handle will capture inbound reassembled IP packets, but not inbound IP fragments.
            /// This flag only affects inbound packets at the <see cref="Layer.Network"/> layer, else the flag is ignored.
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
            /// Do not calculate the IPv4 checksum.
            /// </summary>
            NoIPv4Checksum = 1,

            /// <summary>
            /// Do not calculate the ICMP checksum.
            /// </summary>
            NoICMPv4Checksum = 2,

            /// <summary>
            /// Do not calculate the ICMPv6 checksum.
            /// </summary>
            NoICMPv6Checksum = 4,

            /// <summary>
            /// Do not calculate the TCP checksum.
            /// </summary>
            NoTCPChecksum = 8,

            /// <summary>
            /// Do not calculate the UDP checksum.
            /// </summary>
            NoUDPChecksum = 16,
        }
    }

    internal class SafeWinDivertHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeWinDivertHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle) => SetHandle(existingHandle);
        protected override bool ReleaseHandle() => NativeMethods.WinDivertClose(handle);
    }

    /// <summary>
    /// Retrieves a raw handle from a <see cref="SafeHandle"/>.
    /// </summary>
    public struct SafeHandleReference : IDisposable
    {
        /// <summary>
        /// Handle taken from <see cref="SafeHandle"/>.
        /// If the handle has already been closed, it will be set to an invalid handle value.
        /// </summary>
        public IntPtr RawHandle { get; private set; }

        private readonly SafeHandle? handle;
        private readonly IntPtr invalid;
        private bool reference;

        /// <summary>
        /// Initializes an instance of <see cref="SafeHandleReference"/> class.
        /// </summary>
        /// <param name="handle">The target <see cref="SafeHandle"/>.</param>
        /// <param name="invalid">Invalid value for handle. The value to be used instead of the actual handle if <paramref name="handle"/> is already closed.</param>
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

        /// <summary>
        /// Releases the underlying <see cref="SafeHandle"/>.
        /// The user must call this function. Otherwise, the handle will leak.
        /// </summary>
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
    /// Throws exceptions raised by operations on WinDivert handle.
    /// </summary>
    public class WinDivertException : Win32Exception
    {
        /// <summary>
        /// Get the name of the native function that caused this error.
        /// </summary>
        public string WinDivertNativeMethod { get; }

        internal WinDivertException(string method) : base() => WinDivertNativeMethod = method;
    }

    /// <summary>
    /// It is the same as <see cref="WinDivertPacketParser"/> except for the indexes that are added to the result.
    /// </summary>
    public struct WinDivertIndexedPacketParser : IEnumerable<(int, WinDivertParseResult)>
    {
        private readonly WinDivertPacketParser e;

        /// <summary>
        /// Initializes an instance of <see cref="WinDivertIndexedPacketParser"/> class with given packets.
        /// </summary>
        /// <param name="packet">Packets to be parsed.</param>
        public WinDivertIndexedPacketParser(Memory<byte> packet) => e = new WinDivertPacketParser(packet);

        /// <summary>
        /// Initializes an instance of the <see cref="WinDivertIndexedPacketParser"/> class that wraps the given <see cref="WinDivertPacketParser"/>.
        /// </summary>
        /// <param name="e"><see cref="WinDivertPacketParser"/> to be wrapped.</param>
        public WinDivertIndexedPacketParser(WinDivertPacketParser e) => this.e = e;

        /// <summary>
        /// Returns an enumerator that iterates over the results of packet parsing.
        /// Since this function returns the struct as is, no boxing occurs and heap allocation can be avoided.
        /// </summary>
        /// <returns>An enumerator that iterates the result of packet parsing.</returns>
        public WinDivertIndexedPacketEnumerator GetEnumerator() => new(e.GetEnumerator());

        IEnumerator<(int, WinDivertParseResult)> IEnumerable<(int, WinDivertParseResult)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// It is the same as <see cref="WinDivertPacketEnumerator"/> except for the indexes that are added to the result.
    /// </summary>
    /// <remarks>
    /// Since the given packet is pinned, it is safe to dereference the pointer in the parsed result during enumeration.
    /// Do not use the pointer after the enumeration is finished.
    /// </remarks>
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

        /// <summary>
        /// Releases the underlying resources.
        /// The user must call this function. Otherwise, the packet buffer will not be unpinned.
        /// </summary>
        public void Dispose() => e.Dispose();
    }

    /// <summary>
    /// An enumerable that parses the given packet.
    /// </summary>
    public struct WinDivertPacketParser : IEnumerable<WinDivertParseResult>
    {
        private readonly Memory<byte> packet;

        /// <summary>
        /// Initializes an instance of <see cref="WinDivertPacketParser"/> class.
        /// </summary>
        /// <param name="packet">Packets to be parsed.</param>
        public WinDivertPacketParser(Memory<byte> packet) => this.packet = packet;

        /// <summary>
        /// Returns an enumerator that iterates over the results of packet parsing.
        /// Since this function returns the struct as is, no boxing occurs and heap allocation can be avoided.
        /// </summary>
        /// <returns>An enumerator that iterates the result of packet parsing.</returns>
        public WinDivertPacketEnumerator GetEnumerator() => new(packet);

        IEnumerator<WinDivertParseResult> IEnumerable<WinDivertParseResult>.GetEnumerator() => new WinDivertPacketEnumerator(packet);
        IEnumerator IEnumerable.GetEnumerator() => new WinDivertPacketEnumerator(packet);
    }

    /// <summary>
    /// An enumerator that parses the given packet.
    /// </summary>
    /// <remarks>
    /// Since the given packet is pinned, it is safe to dereference the pointer in the parsed result during enumeration.
    /// Do not use the pointer after the enumeration is finished.
    /// </remarks>
    public unsafe struct WinDivertPacketEnumerator : IEnumerator<WinDivertParseResult>
    {
        private readonly MemoryHandle hmem;
        private readonly Memory<byte> packet;
        private readonly byte* pPacket0;
        private byte* pPacket;
        private uint packetLen;

        private WinDivertParseResult current;
        public WinDivertParseResult Current => current;
        object IEnumerator.Current => current;

        internal WinDivertPacketEnumerator(Memory<byte> packet)
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
                : Memory<byte>.Empty;

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

        /// <summary>
        /// Releases the underlying resources.
        /// The user must call this function. Otherwise, the packet buffer will not be unpinned.
        /// </summary>
        public void Dispose() => hmem.Dispose();
    }

    /// <summary>
    /// The result of packet parsing.
    /// </summary>
    public unsafe struct WinDivertParseResult
    {
        /// <summary>
        /// The entirety of the packet.
        /// </summary>
        public Memory<byte> Packet;

        /// <summary>
        /// Points to the IPv4 header of the packet.
        /// If the packet does not contain any IPv4 header, it will be null.
        /// </summary>
        public WinDivertIPv4Hdr* IPv4Hdr;

        /// <summary>
        /// Points to the IPv6 header of the packet.
        /// If the packet does not contain any IPv6 header, it will be null.
        /// </summary>
        public WinDivertIPv6Hdr* IPv6Hdr;

        /// <summary>
        /// Transport protocol.
        /// </summary>
        public byte Protocol;

        /// <summary>
        /// Points to the ICMPv4 header of the packet.
        /// If the packet does not contain any ICMPv4 header, it will be null.
        /// </summary>
        public WinDivertICMPv4Hdr* ICMPv4Hdr;

        /// <summary>
        /// Points to the ICMPv6 header of the packet.
        /// If the packet does not contain any ICMPv6 header, it will be null.
        /// </summary>
        public WinDivertICMPv6Hdr* ICMPv6Hdr;

        /// <summary>
        /// Points to the TCP header of the packet.
        /// If the packet does not contain any TCP header, it will be null.
        /// </summary>
        public WinDivertTCPHdr* TCPHdr;

        /// <summary>
        /// Points to the UDP header of the packet.
        /// If the packet does not contain any UDP header, it will be null.
        /// </summary>
        public WinDivertUDPHdr* UDPHdr;

        /// <summary>
        /// The packet's data/payload.
        /// If the packet does not contain any data/payload, it will be empty.
        /// </summary>
        public Memory<byte> Data;
    }

    /// <summary>
    /// An exception thrown when a packet filter string is invalid.
    /// </summary>
    public class WinDivertInvalidFilterException : ArgumentException
    {
        /// <summary>
        /// The error description.
        /// </summary>
        public string FilterErrorStr;

        /// <summary>
        /// The error position.
        /// </summary>
        public uint FilterErrorPos;

        /// <summary>
        /// Initializes an new instance of <see cref="WinDivertInvalidFilterException"/> class.
        /// </summary>
        /// <param name="errorStr">The error description.</param>
        /// <param name="errorPos">The error position.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public WinDivertInvalidFilterException(string errorStr, uint errorPos, string? paramName) : base(errorStr, paramName)
        {
            FilterErrorStr = errorStr;
            FilterErrorPos = errorPos;
        }
    }

    /// <summary>
    /// IPv4 address in host byte-order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct IPv4Addr : IEquatable<IPv4Addr>
    {
        internal uint Raw;

        /// <summary>
        /// Parses an IPv4 address stored in <paramref name="addrStr"/>.
        /// </summary>
        /// <param name="addrStr">The address string.</param>
        /// <returns>Output address.</returns>
        /// <exception cref="WinDivertException">Thrown if the parse fails.</exception>
        public static unsafe IPv4Addr Parse(string addrStr)
        {
            var addr = new IPv4Addr();
            var success = NativeMethods.WinDivertHelperParseIPv4Address(addrStr, &addr.Raw);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertHelperParseIPv4Address));
            return addr;
        }

        /// <summary>
        /// Convert an IPv4 address into a string.
        /// </summary>
        /// <returns>The formatted string.</returns>
        /// <exception cref="WinDivertException">Thrown if formatting fails.</exception>
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
    /// IPv4 address in network byte-order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NetworkIPv4Addr : IEquatable<NetworkIPv4Addr>
    {
        internal uint Raw;

        /// <summary>
        /// Convert an IPv4 address into a string.
        /// </summary>
        /// <returns>The formatted string.</returns>
        /// <exception cref="WinDivertException">Thrown if formatting fails.</exception>
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
    /// IPv6 address in host byte-order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IPv6Addr : IEquatable<IPv6Addr>
    {
        internal fixed uint Raw[4];

        /// <summary>
        /// Parses an IPv6 address stored in <paramref name="addrStr"/>.
        /// </summary>
        /// <param name="addrStr">The address string.</param>
        /// <returns>Output address.</returns>
        /// <exception cref="WinDivertException">Thrown if the parse fails.</exception>
        public static IPv6Addr Parse(string addrStr)
        {
            var addr = new IPv6Addr();
            var success = NativeMethods.WinDivertHelperParseIPv6Address(addrStr, addr.Raw);
            if (!success) throw new WinDivertException(nameof(NativeMethods.WinDivertHelperParseIPv6Address));
            return addr;
        }

        /// <summary>
        /// Convert an IPv6 address into a string.
        /// </summary>
        /// <returns>The formatted string.</returns>
        /// <exception cref="WinDivertException">Thrown if formatting fails.</exception>
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
    /// IPv6 address in network byte-order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NetworkIPv6Addr : IEquatable<NetworkIPv6Addr>
    {
        internal fixed uint Raw[4];

        /// <summary>
        /// Convert an IPv6 address into a string.
        /// </summary>
        /// <returns>The formatted string.</returns>
        /// <exception cref="WinDivertException">Thrown if formatting fails.</exception>
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
    /// <see cref="ushort"/> in network byte-order.
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
    /// <see cref="uint"/> in network byte-order.
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
        /// <summary>
        /// A timestamp indicating when event occurred.
        /// </summary>
        [FieldOffset(0)]
        public long Timestamp;

        [FieldOffset(8)] private byte byteLayer;
        [FieldOffset(9)] private byte byteEvent;
        [FieldOffset(10)] private byte flags;

        /// <summary>
        /// Data specific to the network layer.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Layer"/> is not <see cref="WinDivert.Layer.Network"/>, do not access this field.
        /// This field is in a <c>union</c> and shares memory space with other fields.
        /// </remarks>
        [FieldOffset(16)]
        public WinDivertDataNetwork Network;

        /// <summary>
        /// Data specific to the flow layer.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Layer"/> is not <see cref="WinDivert.Layer.Flow"/>, do not access this field.
        /// This field is in a <c>union</c> and shares memory space with other fields.
        /// </remarks>
        [FieldOffset(16)]
        public WinDivertDataFlow Flow;

        /// <summary>
        /// Data specific to the socket layer.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Layer"/> is not <see cref="WinDivert.Layer.Socket"/>, do not access this field.
        /// This field is in a <c>union</c> and shares memory space with other fields.
        /// </remarks>
        [FieldOffset(16)]
        public WinDivertDataSocket Socket;

        /// <summary>
        /// Data specific to the reflect layer.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Layer"/> is not <see cref="WinDivert.Layer.Reflect"/>, do not access this field.
        /// This field is in a <c>union</c> and shares memory space with other fields.
        /// </remarks>
        [FieldOffset(16)]
        public WinDivertDataReflect Reflect;

        [FieldOffset(16)] private fixed byte reserved[64];

        /// <summary>
        /// The handle's layer.
        /// </summary>
        public WinDivert.Layer Layer
        {
            get => (WinDivert.Layer)byteLayer;
            set => byteLayer = (byte)value;
        }

        /// <summary>
        /// The captured event.
        /// </summary>
        public WinDivert.Event Event
        {
            get => (WinDivert.Event)byteEvent;
            set => byteEvent = (byte)value;
        }

        /// <summary>
        /// Set to <c>true</c> if the event was sniffed (i.e., not blocked), <c>false</c> otherwise.
        /// </summary>
        public bool Sniffed
        {
            get => GetFlag(1 << 0);
            set => SetFlag(1 << 0, value);
        }

        /// <summary>
        /// Set to <c>true</c> for outbound packets/event, <c>false</c> for inbound or otherwise.
        /// </summary>
        public bool Outbound
        {
            get => GetFlag(1 << 1);
            set => SetFlag(1 << 1, value);
        }

        /// <summary>
        /// Set to <c>true</c> for loopback packets, <c>false</c> otherwise.
        /// </summary>
        public bool Loopback
        {
            get => GetFlag(1 << 2);
            set => SetFlag(1 << 2, value);
        }

        /// <summary>
        /// Set to <c>true</c> for impostor packets, <c>false</c> otherwise.
        /// </summary>
        public bool Impostor
        {
            get => GetFlag(1 << 3);
            set => SetFlag(1 << 3, value);
        }

        /// <summary>
        /// Set to <c>true</c> for IPv6 packets/events, <c>false</c> otherwise.
        /// </summary>
        public bool IPv6
        {
            get => GetFlag(1 << 4);
            set => SetFlag(1 << 4, value);
        }

        /// <summary>
        /// Set to <c>true</c> if the IPv4 checksum is valid, <c>false</c> otherwise.
        /// </summary>
        public bool IPChecksum
        {
            get => GetFlag(1 << 5);
            set => SetFlag(1 << 5, value);
        }

        /// <summary>
        /// Set to <c>true</c> if the TCP checksum is valid, <c>false</c> otherwise.
        /// </summary>
        public bool TCPChecksum
        {
            get => GetFlag(1 << 6);
            set => SetFlag(1 << 6, value);
        }

        /// <summary>
        /// Set to <c>true</c> if the UDP checksum is valid, <c>false</c> otherwise.
        /// </summary>
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
        /// <summary>
        /// The interface index on which the packet arrived (for inbound packets), or is to be sent (for outbound packets).
        /// </summary>
        public uint IfIdx;

        /// <summary>
        /// The sub-interface index for <see cref="IfIdx"/>.
        /// </summary>
        public uint SubIfIdx;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertDataFlow
    {
        /// <summary>
        /// The endpoint ID of the flow.
        /// </summary>
        public ulong EndpointId;

        /// <summary>
        /// The parent endpoint ID of the flow.
        /// </summary>
        public ulong ParentEndpointId;

        /// <summary>
        /// The ID of the process associated with the flow.
        /// </summary>
        public uint ProcessId;

        /// <summary>
        /// The local address associated with the flow.
        /// </summary>
        public IPv6Addr LocalAddr;

        /// <summary>
        /// The remote address associated with the flow.
        /// </summary>
        public IPv6Addr RemoteAddr;

        /// <summary>
        /// The local port associated with the flow.
        /// </summary>
        public ushort LocalPort;

        /// <summary>
        /// The remote port associated with the flow.
        /// </summary>
        public ushort RemotePort;

        /// <summary>
        /// The protocol associated with the flow.
        /// </summary>
        public byte Protocol;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinDivertDataSocket
    {
        /// <summary>
        /// The endpoint ID of the socket operation.
        /// </summary>
        public ulong EndpointId;

        /// <summary>
        /// The parent endpoint ID of the socket operation.
        /// </summary>
        public ulong ParentEndpointId;

        /// <summary>
        /// The ID of the process associated with the socket operation.
        /// </summary>
        public uint ProcessId;

        /// <summary>
        /// The local address associated with the socket operation.
        /// </summary>
        public IPv6Addr LocalAddr;

        /// <summary>
        /// The remote address associated with the socket operation.
        /// </summary>
        public IPv6Addr RemoteAddr;

        /// <summary>
        /// The local port associated with the socket operation.
        /// </summary>
        public ushort LocalPort;

        /// <summary>
        /// The remote port associated with the socket operation.
        /// </summary>
        public ushort RemotePort;

        /// <summary>
        /// The protocol associated with the socket operation.
        /// </summary>
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
    /// IPv4 header definition.
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
    /// IPv6 header definition.
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
    /// ICMP header definition.
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
    /// ICMPv6 header definition.
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
    /// TCP header definition.
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
    /// UDP header definition.
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