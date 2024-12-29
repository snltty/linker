using MemoryPack;
using linker.messenger.updater;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableUpdaterConfirmInfo
    {
        [MemoryPackIgnore]
        public readonly UpdaterConfirmInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackInclude]
        bool GroupAll => info.GroupAll;

        [MemoryPackInclude]
        bool All => info.All;

        [MemoryPackConstructor]
        SerializableUpdaterConfirmInfo(string machineId, string version, string secretKey, bool groupAll, bool all)
        {
            var info = new UpdaterConfirmInfo { MachineId = machineId, SecretKey = secretKey, All = all, GroupAll = groupAll, Version = version };
            this.info = info;
        }

        public SerializableUpdaterConfirmInfo(UpdaterConfirmInfo info)
        {
            this.info = info;
        }
    }
    public class UpdaterConfirmInfoFormatter : MemoryPackFormatter<UpdaterConfirmInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterConfirmInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableUpdaterConfirmInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterConfirmInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableUpdaterConfirmInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableUpdaterConfirmServerInfo
    {
        [MemoryPackIgnore]
        public readonly UpdaterConfirmServerInfo info;

        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackConstructor]
        SerializableUpdaterConfirmServerInfo(string version, string secretKey)
        {
            var info = new UpdaterConfirmServerInfo { SecretKey = secretKey, Version = version };
            this.info = info;
        }

        public SerializableUpdaterConfirmServerInfo(UpdaterConfirmServerInfo info)
        {
            this.info = info;
        }
    }
    public class UpdaterConfirmServerInfoFormatter : MemoryPackFormatter<UpdaterConfirmServerInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterConfirmServerInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableUpdaterConfirmServerInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterConfirmServerInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableUpdaterConfirmServerInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableUpdateClientInfo
    {
        [MemoryPackIgnore]
        public readonly UpdaterClientInfo info;

        [MemoryPackInclude]
        string[] ToMachines => info.ToMachines;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        UpdaterInfo Info => info.Info;

        [MemoryPackConstructor]
        SerializableUpdateClientInfo(string[] toMachines, UpdaterInfo info)
        {
            this.info = new UpdaterClientInfo { ToMachines = ToMachines, Info = info };
        }

        public SerializableUpdateClientInfo(UpdaterClientInfo info)
        {
            this.info = info;
        }
    }
    public class UpdaterClientInfoFormatter : MemoryPackFormatter<UpdaterClientInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterClientInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableUpdateClientInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterClientInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableUpdateClientInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableUpdateInfo
    {
        [MemoryPackIgnore]
        public readonly UpdaterInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        UpdaterStatus Status => info.Status;

        [MemoryPackInclude]
        long Length => info.Length;

        [MemoryPackInclude]
        long Current => info.Current;

        [MemoryPackConstructor]
        SerializableUpdateInfo(string machineId, UpdaterStatus status, long length, long current)
        {
            this.info = new UpdaterInfo { MachineId = machineId, Status = status, Length = length, Current = current };
        }

        public SerializableUpdateInfo(UpdaterInfo info)
        {
            this.info = info;
        }
    }
    public class UpdaterInfoFormatter : MemoryPackFormatter<UpdaterInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableUpdateInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableUpdateInfo>();
            value = wrapped.info;
        }
    }
}
