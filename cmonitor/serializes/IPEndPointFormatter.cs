using common.libs.extends;
using MemoryPack;
using System.Net;

namespace cmonitor.serializes
{
    public sealed class IPEndPointFormatter : MemoryPackFormatter<IPEndPoint>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IPEndPoint value)
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }


            Memory<byte> memory = new byte[20];
            Span<byte> span = memory.Span;
            int index = 1;

            value.Address.TryWriteBytes(span.Slice(index), out int bytesWritten);
            index += bytesWritten;
            span[0] = (byte)bytesWritten;

            ushort port = (ushort)value.Port;
            port.ToBytes(memory.Slice(index));
            index += 2;

            writer.WriteCollectionHeader(index + 4);
            writer.WriteSpan(span.Slice(0, index));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref IPEndPoint value)
        {
            if (!reader.TryReadCollectionHeader(out int len))
            {
                value = null;
                return;
            }


            Span<byte> span = Array.Empty<byte>();
            reader.ReadSpan(ref span);

            int length = span[4];
            value = new IPEndPoint(new IPAddress(span.Slice(4 + 1, length)), span.Slice(4 + 1 + length).ToUInt16());
        }
    }
}
