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

            var wrapped = reader.ReadPackable<SerializablePlanInfo>();
            value = wrapped.info;
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
        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackConstructor]
        SerializablePlanGetInfo(string machineId, string category, string key)
        {
            var info = new PlanGetInfo
            {
                MachineId = machineId,
                Category = category,
                Key = key
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

            var wrapped = reader.ReadPackable<SerializablePlanGetInfo>();
            value = wrapped.info;
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

            var wrapped = reader.ReadPackable<SerializablePlanAddInfo>();
            value = wrapped.info;
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

            var wrapped = reader.ReadPackable<SerializablePlanRemoveInfo>();
            value = wrapped.info;
        }
    }
}
