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


        [MemoryPackConstructor]
        SerializableSyncInfo(string name, Memory<byte> data)
        {
            var info = new SyncInfo { Name = name, Data = data };
            this.info = info;
        }

        public SerializableSyncInfo(SyncInfo signInfo)
        {
            this.info = signInfo;
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

            var wrapped = reader.ReadPackable<SerializableSyncInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableSync184Info
    {
        [MemoryPackIgnore]
        public readonly Sync184Info info;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        Memory<byte> Data => info.Data;

        [MemoryPackInclude]
        string[] Ids => info.Ids;


        [MemoryPackConstructor]
        SerializableSync184Info(string name, Memory<byte> data, string[] ids)
        {
            var info = new Sync184Info { Name = name, Data = data, Ids = ids };
            this.info = info;
        }

        public SerializableSync184Info(Sync184Info info)
        {
            this.info = info;
        }
    }
    public class Sync184InfoFormatter : MemoryPackFormatter<Sync184Info>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Sync184Info value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSync184Info(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Sync184Info value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSync184Info>();
            value = wrapped.info;
        }
    }
}
