using linker.libs.extends;
using MemoryPack;
using System.Collections;

namespace linker.messenger.serializer.memorypack
{
    public class BitArrayFormatter : MemoryPackFormatter<BitArray>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref BitArray value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(1);
            writer.WriteValue(value.ToBinaryStringFast());
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref BitArray value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new BitArray(reader.ReadValue<string>().Select(c => c == '1').ToArray());
        }
    }
}
