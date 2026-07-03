using linker.messenger.sync;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    public class SyncInfoFormatter : MemoryPackFormatter<SyncInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SyncInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Data);
            writer.WriteValue(value.Ids);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SyncInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }
            value = new SyncInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Name = reader.ReadValue<string>();
            value.Data = reader.ReadValue<Memory<byte>>();
            if (count > 2)
                value.Ids = reader.ReadValue<string[]>();
        }
    }
}
