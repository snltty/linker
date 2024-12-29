using MemoryPack;
using linker.messenger.access;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableAccessUpdateInfo
    {
        [MemoryPackIgnore]
        public readonly AccessUpdateInfo info;

        [MemoryPackInclude]
        string FromMachineId => info.FromMachineId;

        [MemoryPackInclude]
        string ToMachineId => info.ToMachineId;

        [MemoryPackInclude]
        ulong Access => info.Access;

        [MemoryPackConstructor]
        SerializableAccessUpdateInfo(string fromMachineId, string toMachineId, ulong access)
        {
            var info = new AccessUpdateInfo { FromMachineId = fromMachineId, ToMachineId = toMachineId, Access = access };
            this.info = info;
        }

        public SerializableAccessUpdateInfo(AccessUpdateInfo info)
        {
            this.info = info;
        }
    }
    public class AccessUpdateInfoFormatter : MemoryPackFormatter<AccessUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableAccessUpdateInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableAccessUpdateInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableAccessInfo
    {
        [MemoryPackIgnore]
        public readonly AccessInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        AccessValue Access => info.Access;

        [MemoryPackConstructor]
        SerializableAccessInfo(string machineId, AccessValue access)
        {
            var info = new AccessInfo {  MachineId= machineId, Access = access };
            this.info = info;
        }

        public SerializableAccessInfo(AccessInfo info)
        {
            this.info = info;
        }
    }
    public class AccessInfoFormatter : MemoryPackFormatter<AccessInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableAccessInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableAccessInfo>();
            value = wrapped.info;
        }
    }
}
