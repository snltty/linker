using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace linker.tunnel.connection
{
    public struct ChannelPacket
    {
        public byte[] buffer;
        public int length;

        public ChannelPacket(byte[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }
    }

    public sealed class UdpWSASender : IDisposable
    {
        private const int MaxBatchPackets = 64;
        private const int MaxUsoPayload = 65_507;
        private const SocketOptionName UdpSendMessageSize = SocketOptionName.AcceptConnection;

        public Socket Socket { get; }
        public IPEndPoint RemoteEndPoint { get; }

        private readonly ArrayPool<byte> pool;
        private readonly object batchGate = new();
        private readonly object sendGate;
        private readonly ChannelPacket[] batch = new ChannelPacket[MaxBatchPackets];
        private readonly MemoryHandle[] handles = new MemoryHandle[MaxBatchPackets];

        private GCHandle addressHandle;
        private readonly IntPtr addressPtr;
        private readonly int addressSize;

        private bool usoAvailable = OperatingSystem.IsWindows();
        private bool sendMmsgAvailable = OperatingSystem.IsLinux();
        private bool disposed;

        public Channel<ChannelPacket> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<ChannelPacket>(new BoundedChannelOptions(8192)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        public UdpWSASender(
            Socket socket,
            IPEndPoint remoteEndPoint,
            ArrayPool<byte>? arrayPool = null,
            bool usoAvailable = true,
            object? sendGate = null)
        {
            ArgumentNullException.ThrowIfNull(socket);
            ArgumentNullException.ThrowIfNull(remoteEndPoint);

            if (socket.SocketType != SocketType.Dgram ||
                socket.ProtocolType != ProtocolType.Udp)
            {
                throw new ArgumentException("Socket must be UDP.", nameof(socket));
            }

            Socket = socket;
            RemoteEndPoint = remoteEndPoint;
            pool = arrayPool ?? ArrayPool<byte>.Shared;
            this.sendGate = sendGate ?? socket;

            var nativeAddress = CreateNativeSocketAddress(socket, remoteEndPoint);
            addressHandle = GCHandle.Alloc(nativeAddress, GCHandleType.Pinned);
            addressPtr = addressHandle.AddrOfPinnedObject();
            addressSize = nativeAddress.Length;

            this.usoAvailable = this.usoAvailable && usoAvailable;
        }

        public int SendAvailable(ChannelReader<ChannelPacket> reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            lock (batchGate)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(UdpWSASender));

                var count = Drain(reader);
                if (count == 0)
                    return 0;

                try
                {
                    // UDP_SEND_MSG_SIZE is socket-wide, so every send path sharing
                    // this socket must use the same gate.
                    lock (sendGate)
                    {
                        if (TrySendWithWindowsUso(count))
                            return count;

                        if (TrySendWithLinuxSendMmsg(count))
                            return count;

                        SendOneByOne(count);
                        return count;
                    }
                }
                finally
                {
                    ReturnBatch(count);
                }
            }
        }

        private int Drain(ChannelReader<ChannelPacket> reader)
        {
            var groupForUso = usoAvailable;
            var count = 0;
            var segmentSize = 0;
            var totalLength = 0;

            try
            {
                while (count < MaxBatchPackets)
                {
                    if (groupForUso && count > 0)
                    {
                        if (!reader.TryPeek(out var next) ||
                            next.length != segmentSize ||
                            totalLength + next.length > MaxUsoPayload)
                        {
                            break;
                        }
                    }

                    if (!reader.TryRead(out var packet))
                        break;

                    if (packet.buffer is null ||
                        packet.length < 0 ||
                        packet.length > packet.buffer.Length)
                    {
                        if (packet.buffer is not null)
                            pool.Return(packet.buffer);

                        throw new InvalidOperationException("Invalid ChannelPacket.");
                    }

                    batch[count++] = packet;

                    if (count == 1)
                        segmentSize = packet.length;

                    totalLength += packet.length;
                }

                return count;
            }
            catch
            {
                ReturnBatch(count);
                throw;
            }
        }

        private unsafe bool TrySendWithWindowsUso(int count)
        {
            if (!usoAvailable || count < 2)
                return false;

            int segmentSize = batch[0].length;
            if (segmentSize <= 0)
                return false;

            int totalLength = segmentSize * count;
            for (var i = 0; i < count; i++)
            {
                if (batch[i].length != segmentSize)
                    return false;
            }

            var previousSendMessageSize = 0;
            bool usoEnabled = false;

            try
            {
                try
                {
                    previousSendMessageSize = Convert.ToInt32(Socket.GetSocketOption(
                        SocketOptionLevel.Udp,
                        UdpSendMessageSize));

                    Socket.SetSocketOption(
                        SocketOptionLevel.Udp,
                        UdpSendMessageSize,
                        segmentSize);

                    usoEnabled = true;
                }
                catch (SocketException ex) when (IsUsoUnsupported(ex.SocketErrorCode))
                {
                    usoAvailable = false;
                    return false;
                }
                PinBatch(count);
                var buffers = stackalloc WSABUF[count];
                PopulateWsaBuffers(buffers, count);

                var result = WSASendTo(
                    Socket.Handle,
                    (IntPtr)buffers,
                    (uint)count,
                    out var bytesSent,
                    SocketFlags.None,
                    addressPtr,
                    addressSize,
                    IntPtr.Zero,
                    IntPtr.Zero);
                if (result == 0)
                {
                    if (bytesSent != (uint)totalLength)
                        throw new SocketException((int)SocketError.NoBufferSpaceAvailable);

                    return true;
                }

                var error = (SocketError)Marshal.GetLastPInvokeError();
                if (IsUsoUnsupported(error))
                {
                    usoAvailable = false;
                    return false;
                }

                throw new SocketException((int)error);
            }
            catch (SocketException ex) when (IsUsoUnsupported(ex.SocketErrorCode))
            {
                usoAvailable = false;
                return false;
            }
            finally
            {
                UnpinBatch(count);

                if (usoEnabled)
                {
                    Socket.SetSocketOption(
                        SocketOptionLevel.Udp,
                        UdpSendMessageSize,
                        previousSendMessageSize);
                }
            }
        }

        private unsafe bool TrySendWithLinuxSendMmsg(int count)
        {
            if (!sendMmsgAvailable || count < 2)
                return false;

            try
            {
                PinBatch(count);

                var vectors = stackalloc IOVec[count];
                var messages = stackalloc MMsgHdr[count];

                for (var i = 0; i < count; i++)
                {
                    vectors[i] = new IOVec
                    {
                        Base = (IntPtr)handles[i].Pointer,
                        Length = (nuint)batch[i].length
                    };

                    messages[i] = new MMsgHdr
                    {
                        Header = new MsgHdr
                        {
                            Name = addressPtr,
                            NameLength = (uint)addressSize,
                            Iov = (IntPtr)(vectors + i),
                            IovLength = 1,
                            Control = IntPtr.Zero,
                            ControlLength = 0,
                            Flags = 0
                        },
                        MessageLength = 0
                    };
                }

                var sent = 0;
                while (sent < count)
                {
                    var result = SendMmsg(
                        Socket.Handle.ToInt32(),
                        messages + sent,
                        (uint)(count - sent),
                        0);

                    if (result > 0)
                    {
                        sent += result;
                        continue;
                    }

                    var error = Marshal.GetLastPInvokeError();
                    if (error == 4) // EINTR
                        continue;

                    throw new SocketException(error);
                }

                return true;
            }
            catch (EntryPointNotFoundException)
            {
                sendMmsgAvailable = false;
                return false;
            }
            catch (DllNotFoundException)
            {
                sendMmsgAvailable = false;
                return false;
            }
            catch (SocketException ex) when (IsSendMmsgUnavailable(ex))
            {
                sendMmsgAvailable = false;
                return false;
            }
            finally
            {
                UnpinBatch(count);
            }
        }

        private void SendOneByOne(int count)
        {
            for (var i = 0; i < count; i++)
            {

                var packet = batch[i];

                var written = Socket.SendTo(
                    packet.buffer.AsSpan(0, packet.length),
                    SocketFlags.None,
                    RemoteEndPoint);

                if (written != packet.length)
                    throw new SocketException((int)SocketError.NoBufferSpaceAvailable);
            }
        }

        private unsafe void PinBatch(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var packet = batch[i];
                handles[i] = packet.buffer.AsMemory(0, packet.length).Pin();
            }
        }
        private unsafe void PopulateWsaBuffers(WSABUF* buffers, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var packet = batch[i];
                buffers[i] = new WSABUF
                {
                    len = (uint)packet.length,
                    buf = (IntPtr)handles[i].Pointer
                };
            }
        }
        private void UnpinBatch(int count)
        {
            for (var i = 0; i < count; i++)
            {
                handles[i].Dispose();
                handles[i] = default;
            }
        }
        private void ReturnBatch(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var buffer = batch[i].buffer;
                batch[i] = default;

                if (buffer is not null)
                    pool.Return(buffer, clearArray: false);
            }
        }
        private static bool IsUsoUnsupported(SocketError error) => error is
            SocketError.ProtocolOption or
            SocketError.OperationNotSupported or
            SocketError.InvalidArgument or
            SocketError.MessageSize or
            SocketError.Fault;

        private static bool IsSendMmsgUnavailable(SocketException exception) =>
            // ENOSYS means the kernel lacks sendmmsg. EINVAL means this host
            // rejected the batch ABI, so use the ordinary Socket path instead.
            exception.ErrorCode is 38 or 22;

        private static byte[] CreateNativeSocketAddress(Socket socket, IPEndPoint endpoint)
        {
            if (socket.AddressFamily == AddressFamily.InterNetwork &&
                endpoint.AddressFamily == AddressFamily.InterNetwork)
            {
                var address = new byte[16];

                BitConverter.TryWriteBytes(
                    address.AsSpan(0, 2),
                    GetNativeAddressFamily(AddressFamily.InterNetwork));

                BinaryPrimitives.WriteUInt16BigEndian(
                    address.AsSpan(2, 2),
                    (ushort)endpoint.Port);

                endpoint.Address.GetAddressBytes()
                    .AsSpan()
                    .CopyTo(address.AsSpan(4, 4));

                return address;
            }

            if (socket.AddressFamily == AddressFamily.InterNetworkV6 &&
                endpoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var address = new byte[28];

                BitConverter.TryWriteBytes(
                    address.AsSpan(0, 2),
                    GetNativeAddressFamily(AddressFamily.InterNetworkV6));

                BinaryPrimitives.WriteUInt16BigEndian(
                    address.AsSpan(2, 2),
                    (ushort)endpoint.Port);

                endpoint.Address.GetAddressBytes()
                    .AsSpan()
                    .CopyTo(address.AsSpan(8, 16));

                BitConverter.TryWriteBytes(
                    address.AsSpan(24, 4),
                    checked((uint)endpoint.Address.ScopeId));

                return address;
            }

            if (socket.AddressFamily == AddressFamily.InterNetworkV6 &&
                endpoint.AddressFamily == AddressFamily.InterNetwork &&
                socket.DualMode)
            {
                var address = new byte[28];

                BitConverter.TryWriteBytes(
                    address.AsSpan(0, 2),
                    GetNativeAddressFamily(AddressFamily.InterNetworkV6));

                BinaryPrimitives.WriteUInt16BigEndian(
                    address.AsSpan(2, 2),
                    (ushort)endpoint.Port);

                address[18] = 0xff;
                address[19] = 0xff;
                endpoint.Address.GetAddressBytes()
                    .AsSpan()
                    .CopyTo(address.AsSpan(20, 4));

                return address;
            }

            throw new NotSupportedException(
                $"Socket address family {socket.AddressFamily} cannot send to {endpoint.AddressFamily}.");
        }

        private static ushort GetNativeAddressFamily(AddressFamily addressFamily)
        {
            // AddressFamily uses Winsock values. Linux's AF_INET6 is 10, while
            // AddressFamily.InterNetworkV6 is 23. sendmmsg consumes sockaddr
            // directly, so the value must match the host kernel ABI.
            if (OperatingSystem.IsLinux() && addressFamily == AddressFamily.InterNetworkV6)
                return 10;

            return checked((ushort)addressFamily);
        }

        public void Dispose()
        {
            lock (batchGate)
            {
                if (disposed)
                    return;

                disposed = true;

                if (addressHandle.IsAllocated)
                    addressHandle.Free();

            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WSABUF
        {
            public uint len;
            public IntPtr buf;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IOVec
        {
            public IntPtr Base;
            public nuint Length;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MsgHdr
        {
            public IntPtr Name;
            public uint NameLength;
            public IntPtr Iov;
            public nuint IovLength;
            public IntPtr Control;
            public nuint ControlLength;
            public int Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MMsgHdr
        {
            public MsgHdr Header;
            public uint MessageLength;
        }

        [DllImport("ws2_32.dll", EntryPoint = "WSASendTo", SetLastError = true)]
        private static extern unsafe int WSASendTo(
            IntPtr socket,
            IntPtr buffers,
            uint bufferCount,
            out uint bytesSent,
            SocketFlags flags,
            IntPtr remoteAddress,
            int remoteAddressLength,
            IntPtr overlapped,
            IntPtr completionRoutine);

        [DllImport("libc", EntryPoint = "sendmmsg", SetLastError = true)]
        private static extern unsafe int SendMmsg(
            int socket,
            MMsgHdr* messages,
            uint messageCount,
            int flags);
    }
}
