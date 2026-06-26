using System;
using System.Net.Sockets;

namespace linker.forward
{
    public readonly unsafe struct ForwardWritePacket
    {
        public readonly byte HeaderLength = 17;

        public readonly ForwardFlags Flag { get; }
        public readonly ProtocolType ProtocolType { get; }
        public readonly byte BufferSize { get; }

        public readonly ushort Port { get; }

        public readonly uint SrcAddr { get; }
        public readonly ushort SrcPort { get; }
        public readonly uint DstAddr { get; }
        public readonly ushort DstPort { get; }

        public ForwardWritePacket(ReadOnlyMemory<byte> memory)
        {
            fixed (byte* ptr = memory.Span)
            {
                Flag = (ForwardFlags)(*(ptr));
                ProtocolType = (ProtocolType)(*(ptr + 1));
                BufferSize = *(ptr + 2);
                Port = *(ushort*)(ptr + 3);
                SrcAddr = *(uint*)(ptr + 5);
                SrcPort = *(ushort*)(ptr + 9);
                DstAddr = *(uint*)(ptr + 11);
                DstPort = *(ushort*)(ptr + 15);
            }
        }
    }
}
