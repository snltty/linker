using MemoryPack;
using linker.messenger.updater;

namespace linker.messenger.serializer.memorypack
{
    public class UpdaterConfirmInfoFormatter : MemoryPackFormatter<UpdaterConfirmInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterConfirmInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Version);
            writer.WriteValue(string.Empty);
            writer.WriteValue(value.GroupAll);
            writer.WriteValue(value.All);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterConfirmInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new UpdaterConfirmInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            reader.ReadValue<string>();
            value.GroupAll = reader.ReadValue<bool>();
            value.All = reader.ReadValue<bool>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(string.Empty);
            writer.WriteValue(value.Version);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterConfirmServerInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new UpdaterConfirmServerInfo();
            reader.TryReadObjectHeader(out byte count);
            reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.ToMachines);
            writer.WriteValue(value.Info);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterClientInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new UpdaterClientInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ToMachines = reader.ReadValue<string[]>();
            value.Info = reader.ReadValue<UpdaterInfo>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.ToMachines);
            writer.WriteValue(value.Info);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterClientInfo170 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new UpdaterClientInfo170();
            reader.TryReadObjectHeader(out byte count);
            value.ToMachines = reader.ReadValue<string[]>();
            value.Info = reader.ReadValue<UpdaterInfo170>();
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

            writer.WriteObjectHeader(7);
            writer.WriteValue(value.Version);
            writer.WriteValue(value.Msg);
            writer.WriteValue(value.DateTime);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Status);
            writer.WriteValue(value.Length);
            writer.WriteValue(value.Current);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new UpdaterInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Version = reader.ReadValue<string>();
            value.Msg = reader.ReadValue<string[]>();
            value.DateTime = reader.ReadValue<string>();
            value.MachineId = reader.ReadValue<string>();
            value.Status = reader.ReadValue<UpdaterStatus>();
            value.Length = reader.ReadValue<long>();
            value.Current = reader.ReadValue<long>();
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

            writer.WriteObjectHeader(7);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Version);
            writer.WriteValue(value.Status);
            writer.WriteValue(value.Length);
            writer.WriteValue(value.Current);
            writer.WriteValue(value.ServerVersion);
            writer.WriteValue(value.Sync2Server);
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

    public class UpdaterSyncInfoFormatter : MemoryPackFormatter<UpdaterSyncInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref UpdaterSyncInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(string.Empty);
            writer.WriteValue(value.Sync2Server);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref UpdaterSyncInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new UpdaterSyncInfo();
            reader.TryReadObjectHeader(out byte count);
            reader.ReadValue<string>();
            value.Sync2Server = reader.ReadValue<bool>();
        }
    }
}


