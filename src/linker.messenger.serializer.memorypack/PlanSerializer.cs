using linker.messenger.plan;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{

    public class PlanInfoFormatter : MemoryPackFormatter<PlanInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref PlanInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(9);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Category);
            writer.WriteValue(value.Key);
            writer.WriteValue(value.Handle);
            writer.WriteValue(value.Value);
            writer.WriteValue(value.Disabled);
            writer.WriteValue(value.TriggerHandle);
            writer.WriteValue(value.Method);
            writer.WriteValue(value.Rule);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref PlanInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new PlanInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<int>();
            value.Category = reader.ReadValue<string>();
            value.Key = reader.ReadValue<string>();
            value.Handle = reader.ReadValue<string>();
            value.Value = reader.ReadValue<string>();
            value.Disabled = reader.ReadValue<bool>();
            value.TriggerHandle = reader.ReadValue<string>();
            value.Method = reader.ReadValue<PlanMethod>();
            value.Rule = reader.ReadValue<string>();
        }
    }

    public class PlanGetInfoFormatter : MemoryPackFormatter<PlanGetInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref PlanGetInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Category);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref PlanGetInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new PlanGetInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Category = reader.ReadValue<string>();
        }
    }

    public class PlanAddInfoFormatter : MemoryPackFormatter<PlanAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref PlanAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Plan);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref PlanAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new PlanAddInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Plan = reader.ReadValue<PlanInfo>();
        }
    }

    public class PlanRemoveInfoFormatter : MemoryPackFormatter<PlanRemoveInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref PlanRemoveInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.PlanId);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref PlanRemoveInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new PlanRemoveInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.PlanId = reader.ReadValue<int>();
        }
    }
}
