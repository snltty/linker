using linker.tunnel.connection;
using System;
using System.Buffers;
using System.Net.Sockets;

namespace linker.forward
{
    public unsafe sealed class ForwardReadPacket : IDisposable
    {
        private byte* ptr;
        public byte[] Buffer { get; private set; }

        public byte HeaderLength => 17 + 4;

        /// <summary>
        /// 2 length + 1 flag + 1 rsv + 17 header + data
        /// </summary>
        public int Length
        {
            get
            {
                //data length + header length (17) + flag(1) + rsv(1) + length(2)
                return TunnelPacket.ReadLength(Buffer.AsMemory()) + 2;
            }
            set
            {
                //data length + header length (17) + flag(1) + rsv(1)
                TunnelPacket.WriteLength(value + 17 + 2, Buffer);
                Buffer[2] = 0;
                Buffer[3] = 0;
            }
        }

        public ForwardFlags Flag
        {
            get => (ForwardFlags)(*(ptr + TunnelPacket.PacketHeaderSize));
            set { *(ptr + TunnelPacket.PacketHeaderSize) = (byte)value; }
        }
        public ProtocolType ProtocolType
        {
            get => (ProtocolType)(*(ptr + TunnelPacket.PacketHeaderSize + 1));
            set { *(ptr + TunnelPacket.PacketHeaderSize + 1) = (byte)value; }
        }
        public byte BufferSize
        {
            get => *(ptr + TunnelPacket.PacketHeaderSize + 2);
            set { *(ptr + TunnelPacket.PacketHeaderSize + 2) = value; }
        }

        public ushort Port
        {
            get => *(ushort*)(ptr + TunnelPacket.PacketHeaderSize + 3);
            set { *(ushort*)(ptr + TunnelPacket.PacketHeaderSize + 3) = value; }
        }

        public uint SrcAddr
        {
            get => *(uint*)(ptr + TunnelPacket.PacketHeaderSize + 5);
            set { *(uint*)(ptr + TunnelPacket.PacketHeaderSize + 5) = value; }
        }
        public ushort SrcPort
        {
            get => *(ushort*)(ptr + TunnelPacket.PacketHeaderSize + 9);
            set { *(ushort*)(ptr + TunnelPacket.PacketHeaderSize + 9) = value; }
        }
        public uint DstAddr
        {
            get => *(uint*)(ptr + TunnelPacket.PacketHeaderSize + 11);
            set { *(uint*)(ptr + TunnelPacket.PacketHeaderSize + 11) = value; }
        }
        public ushort DstPort
        {
            get => *(ushort*)(ptr + TunnelPacket.PacketHeaderSize + 15);
            set { *(ushort*)(ptr + TunnelPacket.PacketHeaderSize + 15) = value; }
        }

        public ForwardReadPacket(byte[] buffer)
        {
            Buffer = buffer;

            handle = buffer.AsMemory().Pin();
            ptr = (byte*)handle.Pointer;
        }

        MemoryHandle handle;
        public void Dispose()
        {
            handle.Dispose();
        }
    }
}
