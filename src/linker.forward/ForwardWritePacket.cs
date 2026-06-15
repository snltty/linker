using System;
using System.Buffers;
using System.Net.Sockets;

namespace linker.forward
{
    public readonly unsafe struct ForwardWritePacket : IDisposable
    {
        private readonly byte* ptr;

        public readonly byte HeaderLength = 17;

        public readonly ForwardFlags Flag => (ForwardFlags)(*(ptr));
        public readonly ProtocolType ProtocolType => (ProtocolType)(*(ptr + 1));
        public readonly byte BufferSize => *(ptr + 2);

        public readonly ushort Port => *(ushort*)(ptr + 3);

        public readonly uint SrcAddr => *(uint*)(ptr + 5);
        public readonly ushort SrcPort => *(ushort*)(ptr + 9);
        public readonly uint DstAddr => *(uint*)(ptr + 11);
        public readonly ushort DstPort => *(ushort*)(ptr + 15);

        public ForwardWritePacket(ReadOnlyMemory<byte> memory)
        {
            handle = memory.Pin();
            ptr = (byte*)handle.Pointer;
        }

        readonly MemoryHandle handle;
        public void Dispose()
        {
            handle.Dispose();
        }
    }
}
