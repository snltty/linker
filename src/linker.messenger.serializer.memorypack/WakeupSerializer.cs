using linker.messenger.wakeup;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    public class WakeupInfoFormatter : MemoryPackFormatter<WakeupInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(7);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Value);
            writer.WriteValue(value.Content);
            writer.WriteValue(value.Remark);
            writer.WriteValue(value.Running);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WakeupInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.Type = reader.ReadValue<WakeupType>();
            value.Name = reader.ReadValue<string>();
            value.Value = reader.ReadValue<string>();
            value.Content = reader.ReadValue<string>();
            value.Remark = reader.ReadValue<string>();
            value.Running = reader.ReadValue<bool>();
        }
    }

    public class WakeupSearchInfoFormatter : MemoryPackFormatter<WakeupSearchInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupSearchInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.Str);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupSearchInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WakeupSearchInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Type = reader.ReadValue<WakeupType>();
            value.Str = reader.ReadValue<string>();
        }
    }

    public class WakeupSearchForwardInfoFormatter : MemoryPackFormatter<WakeupSearchForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupSearchForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupSearchForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WakeupSearchForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<WakeupSearchInfo>();
        }
    }

    public class WakeupAddForwardInfoFormatter : MemoryPackFormatter<WakeupAddForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupAddForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WakeupAddForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<WakeupInfo>();
        }
    }

    public class WakeupRemoveForwardInfoFormatter : MemoryPackFormatter<WakeupRemoveForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupRemoveForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Id);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WakeupRemoveForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Id = reader.ReadValue<string>();
        }
    }

    public class WakeupSendInfoFormatter : MemoryPackFormatter<WakeupSendInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupSendInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.Value);
            writer.WriteValue(value.Content);
            writer.WriteValue(value.Ms);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupSendInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WakeupSendInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.Type = reader.ReadValue<WakeupType>();
            value.Value = reader.ReadValue<string>();
            value.Content = reader.ReadValue<string>();
            value.Ms = reader.ReadValue<int>();
        }
    }

    public class WakeupSendForwardInfoFormatter : MemoryPackFormatter<WakeupSendForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupSendForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupSendForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WakeupSendForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Data = reader.ReadValue<WakeupSendInfo>();
        }
    }

}
