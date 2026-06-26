using linker.libs.extends;
using System;
using System.Buffers;
using System.Net.Sockets;

namespace linker.forward
{
    public unsafe sealed class ForwardReadPacket : IDisposable
    {
        private readonly byte* ptr;

        public const byte HeaderLength = 17 + 4;
        private readonly byte[] buffer = new byte[HeaderLength];

        private int length = HeaderLength;
        public Memory<byte> Memory => buffer.AsMemory(0, length);

        /// <summary>
        /// 2 length + 1 flag + 1 rsv + 17 header + data
        /// </summary>
        public int Length
        {
            set
            {
                length = value + HeaderLength;
                ((ushort)(length - 2)).ToBytes(buffer.AsMemory());
                buffer[2] = 0;
                buffer[3] = 0;
            }
        }

        public ForwardFlags Flag
        {
            get => (ForwardFlags)(*(ptr));
            set { *(ptr) = (byte)value; }
        }
        public ProtocolType ProtocolType
        {
            get => (ProtocolType)(*(ptr + 1));
            set { *(ptr + 1) = (byte)value; }
        }
        public byte BufferSize
        {
            get => *(ptr + 2);
            set { *(ptr + 2) = value; }
        }

        public ushort Port
        {
            get => *(ushort*)(ptr + 3);
            set { *(ushort*)(ptr + 3) = value; }
        }

        public uint SrcAddr
        {
            get => *(uint*)(ptr + 5);
            set { *(uint*)(ptr + 5) = value; }
        }
        public ushort SrcPort
        {
            get => *(ushort*)(ptr + 9);
            set { *(ushort*)(ptr + 9) = value; }
        }
        public uint DstAddr
        {
            get => *(uint*)(ptr + 11);
            set { *(uint*)(ptr + 11) = value; }
        }
        public ushort DstPort
        {
            get => *(ushort*)(ptr + 15);
            set { *(ushort*)(ptr + 15) = value; }
        }

        public ForwardReadPacket(byte[] buffer)
        {
            this.buffer = buffer;

            handle = buffer.AsMemory().Pin();
            ptr = (byte*)handle.Pointer + 4;
        }

        MemoryHandle handle;
        public void Dispose()
        {
            handle.Dispose();
        }
    }
}
