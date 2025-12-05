using MemoryPack;
using linker.messenger.api;
using System.Collections;

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
            value = new AccessUpdateInfo();
            reader.TryReadObjectHeader(out byte count);
            value.FromMachineId = reader.ReadValue<string>();
            value.ToMachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<ulong>();
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableAccessBitsUpdateInfo
    {
        [MemoryPackIgnore]
        public readonly AccessBitsUpdateInfo info;

        [MemoryPackInclude]
        string FromMachineId => info.FromMachineId;

        [MemoryPackInclude]
        string ToMachineId => info.ToMachineId;

        [MemoryPackInclude]
        BitArray Access => info.Access;

        [MemoryPackInclude]
        bool FullAccess => info.FullAccess;

        [MemoryPackConstructor]
        SerializableAccessBitsUpdateInfo(string fromMachineId, string toMachineId, BitArray access, bool fullAccess)
        {
            var info = new AccessBitsUpdateInfo { FromMachineId = fromMachineId, ToMachineId = toMachineId, Access = access, FullAccess = fullAccess };
            this.info = info;
        }

        public SerializableAccessBitsUpdateInfo(AccessBitsUpdateInfo info)
        {
            this.info = info;
        }
    }
    public class AccessBitsUpdateInfoFormatter : MemoryPackFormatter<AccessBitsUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessBitsUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableAccessBitsUpdateInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessBitsUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new AccessBitsUpdateInfo();
            reader.TryReadObjectHeader(out byte count);
            value.FromMachineId = reader.ReadValue<string>();
            value.ToMachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<BitArray>();
            if (count > 3)
            {
                value.FullAccess = reader.ReadValue<bool>();
            }
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
            var info = new AccessInfo { MachineId = machineId, Access = access };
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

            value = new AccessInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<AccessValue>();
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableAccessBitsInfo
    {
        [MemoryPackIgnore]
        public readonly AccessBitsInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        BitArray Access => info.Access;

        [MemoryPackConstructor]
        SerializableAccessBitsInfo(string machineId, BitArray access)
        {
            var info = new AccessBitsInfo { MachineId = machineId, Access = access };
            this.info = info;
        }

        public SerializableAccessBitsInfo(AccessBitsInfo info)
        {
            this.info = info;
        }
    }
    public class AccessBotsInfoFormatter : MemoryPackFormatter<AccessBitsInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessBitsInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableAccessBitsInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessBitsInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new AccessBitsInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<BitArray>();
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableApiPasswordUpdateInfo
    {
        [MemoryPackIgnore]
        public readonly ApiPasswordUpdateInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string Password => info.Password;

        [MemoryPackConstructor]
        SerializableApiPasswordUpdateInfo(string machineid, string password)
        {
            var info = new ApiPasswordUpdateInfo { MachineId = machineid, Password = password };
            this.info = info;
        }

        public SerializableApiPasswordUpdateInfo(ApiPasswordUpdateInfo info)
        {
            this.info = info;
        }
    }
    public class ApiPasswordUpdateInfoFormatter : MemoryPackFormatter<ApiPasswordUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ApiPasswordUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableApiPasswordUpdateInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ApiPasswordUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ApiPasswordUpdateInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Password = reader.ReadValue<string>();
        }
    }

}
