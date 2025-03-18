using MemoryPack;
using linker.messenger.decenter;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableDecenterSyncInfo
    {
        [MemoryPackIgnore]
        public readonly DecenterSyncInfo info;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        string FromMachineId => info.FromMachineId;
        [MemoryPackInclude]
        string ToMachineId => info.ToMachineId;

        [MemoryPackInclude]
        Memory<byte> Data => info.Data;

        [MemoryPackConstructor]
        SerializableDecenterSyncInfo(string name, string fromMachineId, string toMachineId, Memory<byte> data)
        {
            var info = new DecenterSyncInfo { Name = name, FromMachineId = fromMachineId, ToMachineId = toMachineId, Data = data };
            this.info = info;
        }

        public SerializableDecenterSyncInfo(DecenterSyncInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class DecenterSyncInfoFormatter : MemoryPackFormatter<DecenterSyncInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DecenterSyncInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableDecenterSyncInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DecenterSyncInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableDecenterSyncInfo>();
            value = wrapped.info;
        }
    }

}
