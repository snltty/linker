using MemoryPack;
using linker.messenger.decenter;

namespace linker.messenger.serializer.memorypack
{
    public class DecenterSyncInfoFormatter : MemoryPackFormatter<DecenterSyncInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DecenterSyncInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DecenterSyncInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

           
            value = new DecenterSyncInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Name = reader.ReadValue<string>();
            value.Data = reader.ReadValue<Memory<byte>>();

        }
    }

    public class DecenterPullPageInfoFormatter : MemoryPackFormatter<DecenterPullPageInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DecenterPullPageInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DecenterPullPageInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value =new DecenterPullPageInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Name = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
        }
    }

    public class DecenterPullPageResultInfoFormatter : MemoryPackFormatter<DecenterPullPageResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DecenterPullPageResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.List);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DecenterPullPageResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new DecenterPullPageResultInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.List = reader.ReadValue<List<Memory<byte>>>();
        }
    }
}
