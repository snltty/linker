using linker.messenger.sync;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{



    [MemoryPackable]
    public readonly partial struct SerializableSyncInfo
    {
        [MemoryPackIgnore]
        public readonly SyncInfo info;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        Memory<byte> Data => info.Data;

        [MemoryPackInclude]
        string[] Ids => info.Ids;


        [MemoryPackConstructor]
        SerializableSyncInfo(string name, Memory<byte> data, string[] ids)
        {
            var info = new SyncInfo { Name = name, Data = data, Ids = ids };
            this.info = info;
        }

        public SerializableSyncInfo(SyncInfo info)
        {
            this.info = info;
        }
    }
    public class SyncInfoFormatter : MemoryPackFormatter<SyncInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SyncInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSyncInfo(value));
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
