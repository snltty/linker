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
        string SecretKey => string.Empty;

        [MemoryPackInclude]
        bool GroupAll => info.GroupAll;

        [MemoryPackInclude]
        bool All => info.All;

        [MemoryPackConstructor]
        SerializableUpdaterConfirmInfo(string machineId, string version, string secretKey, bool groupAll, bool all)
        {
            var info = new UpdaterConfirmInfo { MachineId = machineId, All = all, GroupAll = groupAll, Version = version };
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
        string SecretKey => string.Empty;

        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackConstructor]
        SerializableUpdaterConfirmServerInfo(string version, string secretKey)
        {
            var info = new UpdaterConfirmServerInfo { Version = version };
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
            this.info = new UpdaterClientInfo { ToMachines = toMachines, Info = info };
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
    public readonly partial struct SerializableUpdaterClientInfo170
    {
        [MemoryPackIgnore]
        public readonly UpdaterClientInfo170 info;

        [MemoryPackInclude]
        string[] ToMachines => info.ToMachines;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        UpdaterInfo170 Info => info.Info;

        [MemoryPackConstructor]
        SerializableUpdaterClientInfo170(string[] toMachines, UpdaterInfo170 info)
        {
            this.info = new UpdaterClientInfo170 { ToMachines = toMachines, Info = info };
        }

        public SerializableUpdaterClientInfo170(UpdaterClientInfo170 info)
        {
            this.info = info;
        }
    }
    public class UpdaterClientInfo170Formatter : MemoryPackFormatter<UpdaterClientInfo170>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterClientInfo170 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableUpdaterClientInfo170(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterClientInfo170 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableUpdaterClientInfo170>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableUpdateInfo
    {
        [MemoryPackIgnore]
        public readonly UpdaterInfo info;

        [MemoryPackInclude]
        string Version => info.Version;
        [MemoryPackInclude]
        string[] Msg => info.Msg;
        [MemoryPackInclude]
        string DateTime => info.DateTime;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        UpdaterStatus Status => info.Status;

        [MemoryPackInclude]
        long Length => info.Length;

        [MemoryPackInclude]
        long Current => info.Current;

        [MemoryPackConstructor]
        SerializableUpdateInfo(string version, string[] msg, string datetime, string machineId, UpdaterStatus status, long length, long current)
        {
            this.info = new UpdaterInfo { Version = version, Msg = msg, DateTime = datetime, MachineId = machineId, Status = status, Length = length, Current = current };
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

    [MemoryPackable]
    public readonly partial struct SerializableUpdaterInfo170
    {
        [MemoryPackIgnore]
        public readonly UpdaterInfo170 info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackInclude]
        UpdaterStatus Status => info.Status;

        [MemoryPackInclude]
        long Length => info.Length;

        [MemoryPackInclude]
        long Current => info.Current;

        [MemoryPackInclude]
        string ServerVersion => info.ServerVersion;

        [MemoryPackInclude]
        bool Sync2Server => info.Sync2Server;

        [MemoryPackConstructor]
        SerializableUpdaterInfo170(string machineId, string version, UpdaterStatus status, long length, long current, string serverVersion, bool sync2Server)
        {
            this.info = new UpdaterInfo170
            {
                MachineId = machineId,
                Version = version,
                Status = status,
                Length = length,
                Current = current,
                ServerVersion = serverVersion,
                Sync2Server = sync2Server
            };
        }

        public SerializableUpdaterInfo170(UpdaterInfo170 info)
        {
            this.info = info;
        }
    }
    public class UpdaterInfo170Formatter : MemoryPackFormatter<UpdaterInfo170>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterInfo170 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableUpdaterInfo170(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterInfo170 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new UpdaterInfo170();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            value.Status = reader.ReadValue<UpdaterStatus>();
            value.Length = reader.ReadValue<long>();
            value.Current = reader.ReadValue<long>();
            if (count > 5)
                value.ServerVersion = reader.ReadValue<string>();
            if (count > 6)
                value.Sync2Server = reader.ReadValue<bool>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableUpdaterSyncInfo
    {
        [MemoryPackIgnore]
        public readonly UpdaterSyncInfo info;

        [MemoryPackInclude]
        string SecretKey => string.Empty;

        [MemoryPackInclude]
        bool Sync2Server => info.Sync2Server;

        [MemoryPackConstructor]
        SerializableUpdaterSyncInfo(string secretKey, bool sync2Server)
        {
            var info = new UpdaterSyncInfo { Sync2Server = sync2Server };
            this.info = info;
        }

        public SerializableUpdaterSyncInfo(UpdaterSyncInfo info)
        {
            this.info = info;
        }
    }
    public class UpdaterSyncInfoFormatter : MemoryPackFormatter<UpdaterSyncInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterSyncInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableUpdaterSyncInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterSyncInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableUpdaterSyncInfo>();
            value = wrapped.info;
        }
    }
}


