using linker.libs.extends;
using System;
using System.Buffers;
using System.Net.Sockets;

namespace linker.forward
{
    public unsafe sealed class ForwardReadPacket : IDisposable
    {
        private byte* ptr;
        public byte[] Buffer { get; set; }
        public int Offset { get; set; }
        public int Length
        {
            get
            {
                return Buffer.ToInt32() + 4;
            }
            set
            {
                (value - 4).ToBytes(Buffer.AsMemory());
                Buffer[2] = 0;
                Buffer[3] = 0;
            }
        }
        public ForwardFlags Flag
        {
            get
            {
                return (ForwardFlags)(*(ptr + 4));
            }
            set
            {
                *(ptr + 4) = (byte)value;
            }
        }
        public ProtocolType ProtocolType
        {
            get
            {
                return (ProtocolType)(*(ptr + 5));
            }
            set
            {
                *(ptr + 5) = (byte)value;
            }
        }
        public byte BufferSize
        {
            get
            {
                return *(ptr + 6);
            }
            set
            {
                *(ptr + 6) = value;
            }
        }
        public ushort Port
        {
            get
            {
                return *(ushort*)(ptr + 7);
            }
            set
            {
                *(ushort*)(ptr + 7) = value;
            }
        }
        public uint SrcAddr
        {
            get
            {
                return *(uint*)(ptr + 9);
            }
            set
            {
                *(uint*)(ptr + 9) = value;
            }
        }
        public ushort SrcPort
        {
            get
            {
                return *(ushort*)(ptr + 13);
            }
            set
            {
                *(ushort*)(ptr + 13) = value;
            }
        }
        public uint DstAddr
        {
            get
            {
                return *(uint*)(ptr + 15);
            }
            set
            {
                *(uint*)(ptr + 15) = value;
            }
        }
        public ushort DstPort
        {
            get
            {
                return *(ushort*)(ptr + 19);
            }
            set
            {
                *(ushort*)(ptr + 19) = value;
            }
        }
        public byte HeaderLength
        {
            get => *(ptr + 21);
            private set
            {
                *(ptr + 21) = value;
            }
        }
        public ForwardReadPacket(byte[] buffer)
        {
            Buffer = buffer;

            handle = buffer.AsMemory().Pin();
            ptr = (byte*)handle.Pointer;

            HeaderLength = 22;
        }

        MemoryHandle handle;
        public void Dispose()
        {
            handle.Dispose();
        }
    }
}
