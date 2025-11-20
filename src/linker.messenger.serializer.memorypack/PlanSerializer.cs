using linker.messenger.plan;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{


    [MemoryPackable]
    public readonly partial struct SerializablePlanInfo
    {
        [MemoryPackIgnore]
        public readonly PlanInfo info;

        [MemoryPackInclude]
        int Id => info.Id;

        [MemoryPackInclude]
        string Category => info.Category;
        [MemoryPackInclude]
        string Key => info.Key;
        [MemoryPackInclude]
        string Handle => info.Handle;
        [MemoryPackInclude]
        string Value => info.Value;
        [MemoryPackInclude]
        bool Disabled => info.Disabled;
        [MemoryPackInclude]
        string TriggerHandle => info.TriggerHandle;
        [MemoryPackInclude]
        PlanMethod Method => info.Method;
        [MemoryPackInclude]
        string Rule => info.Rule;

        [MemoryPackConstructor]
        SerializablePlanInfo(int id, string category, string key, string handle, string value, bool disabled, string triggerHandle, PlanMethod method, string rule)
        {
            var info = new PlanInfo
            {
                Id = id,
                Category = category,
                Key = key,
                Handle = handle,
                Value = value,
                Disabled = disabled,
                TriggerHandle = triggerHandle,
                Method = method,
                Rule = rule
            };
            this.info = info;
        }

        public SerializablePlanInfo(PlanInfo info)
        {
            this.info = info;
        }
    }
    public class PlanInfoFormatter : MemoryPackFormatter<PlanInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref PlanInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializablePlanInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializablePlanGetInfo
    {
        [MemoryPackIgnore]
        public readonly PlanGetInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string Category => info.Category;

        [MemoryPackConstructor]
        SerializablePlanGetInfo(string machineId, string category)
        {
            var info = new PlanGetInfo
            {
                MachineId = machineId,
                Category = category
            };
            this.info = info;
        }

        public SerializablePlanGetInfo(PlanGetInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializablePlanGetInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializablePlanAddInfo
    {
        [MemoryPackIgnore]
        public readonly PlanAddInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        PlanInfo Plan => info.Plan;

        [MemoryPackConstructor]
        SerializablePlanAddInfo(string machineId, PlanInfo plan)
        {
            var info = new PlanAddInfo
            {
                MachineId = machineId,
                Plan = plan
            };
            this.info = info;
        }

        public SerializablePlanAddInfo(PlanAddInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializablePlanAddInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializablePlanRemoveInfo
    {
        [MemoryPackIgnore]
        public readonly PlanRemoveInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        int PlanId => info.PlanId;

        [MemoryPackConstructor]
        SerializablePlanRemoveInfo(string machineId, int planid)
        {
            var info = new PlanRemoveInfo
            {
                MachineId = machineId,
                PlanId = planid
            };
            this.info = info;
        }

        public SerializablePlanRemoveInfo(PlanRemoveInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializablePlanRemoveInfo(value));
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
