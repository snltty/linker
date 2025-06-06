using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    /// <summary>
    /// MemoryPack 的 IPAddress序列化扩展
    /// </summary>
    public sealed class IPAddressFormatter : MemoryPackFormatter<IPAddress>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IPAddress value)
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            //最多是IPV6 16字节+加头部4字节
            Span<byte> span = stackalloc byte[20];

            value.TryWriteBytes(span.Slice(1), out int bytesWritten);

            span[0] = (byte)bytesWritten;

            writer.WriteCollectionHeader(bytesWritten + 4);
            writer.WriteSpan(span.Slice(0, bytesWritten + 1));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref IPAddress value)
        {
            if (!reader.TryReadCollectionHeader(out int len))
            {
                value = null;
                return;
            }


            Span<byte> span = Array.Empty<byte>();
            reader.ReadSpan(ref span);

            int length = span[0];
            value = new IPAddress(span.Slice(0 + 1, length));
        }
    }
}
