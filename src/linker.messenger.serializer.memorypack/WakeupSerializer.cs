using linker.messenger.wakeup;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableWakeupInfo
    {
        [MemoryPackIgnore]
        public readonly WakeupInfo info;

        [MemoryPackInclude]
        string Id => info.Id;

        [MemoryPackInclude]
        WakeupType Type => info.Type;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        string Value => info.Value;

        [MemoryPackInclude]
        string Content => info.Content;

        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackConstructor]
        SerializableWakeupInfo(string id, WakeupType type, string name, string value, string content, string remark)
        {
            var info = new WakeupInfo
            {
                Id = id,
                Type = type,
                Name = name,
                Value = value,
                Content = content,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableWakeupInfo(WakeupInfo info)
        {
            this.info = info;
        }
    }
    public class WakeupInfoFormatter : MemoryPackFormatter<WakeupInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WakeupInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableWakeupInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWakeupInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableWakeupSearchInfo
    {
        [MemoryPackIgnore]
        public readonly WakeupSearchInfo info;

        [MemoryPackInclude]
        WakeupType Type => info.Type;

        [MemoryPackInclude]
        string Str => info.Str;

        [MemoryPackConstructor]
        SerializableWakeupSearchInfo(WakeupType type, string str)
        {
            var info = new WakeupSearchInfo
            {
                Type = type,
                Str = str,
            };
            this.info = info;
        }

        public SerializableWakeupSearchInfo(WakeupSearchInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableWakeupSearchInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupSearchInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWakeupSearchInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableWakeupSearchForwardInfo
    {
        [MemoryPackIgnore]
        public readonly WakeupSearchForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        WakeupSearchInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableWakeupSearchForwardInfo(string machineId, WakeupSearchInfo data)
        {
            this.info = new WakeupSearchForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableWakeupSearchForwardInfo(WakeupSearchForwardInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableWakeupSearchForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupSearchForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWakeupSearchForwardInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableWakeupAddForwardInfo
    {
        [MemoryPackIgnore]
        public readonly WakeupAddForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        WakeupInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableWakeupAddForwardInfo(string machineId, WakeupInfo data)
        {
            this.info = new WakeupAddForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableWakeupAddForwardInfo(WakeupAddForwardInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableWakeupAddForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWakeupAddForwardInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableWakeupRemoveForwardInfo
    {
        [MemoryPackIgnore]
        public readonly WakeupRemoveForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        string Id => info.Id;

        [MemoryPackConstructor]
        SerializableWakeupRemoveForwardInfo(string machineId, string id)
        {
            this.info = new WakeupRemoveForwardInfo
            {
                MachineId = machineId,
                Id = id
            };
        }

        public SerializableWakeupRemoveForwardInfo(WakeupRemoveForwardInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableWakeupRemoveForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WakeupRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWakeupRemoveForwardInfo>();
            value = wrapped.info;
        }
    }

}
