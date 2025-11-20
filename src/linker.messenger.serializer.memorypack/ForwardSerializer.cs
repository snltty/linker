using MemoryPack;
using System.Net;
using linker.messenger.forward;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableForwardInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardInfo info;

        [MemoryPackInclude]
        long Id => info.Id;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string GroupId => info.GroupId;

        [MemoryPackInclude]
        string MachineName => info.MachineName;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress BindIPAddress => info.BindIPAddress;

        [MemoryPackInclude]
        int Port => info.Port;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint TargetEP => info.TargetEP;

        [MemoryPackInclude]
        bool Started => info.Started;

        [MemoryPackInclude]
        byte BufferSize => info.BufferSize;

        [MemoryPackInclude]
        string Msg => info.Msg;

        [MemoryPackInclude]
        string TargetMsg => info.TargetMsg;

        [MemoryPackConstructor]
        SerializableForwardInfo(long id, string name, string machineId, string groupId, string machineName, IPAddress bindIPAddress, int port, IPEndPoint targetEP,
            bool started, byte bufferSize, string msg, string targetMsg)
        {
            this.info = new ForwardInfo
            {
                Name = name,
                BufferSize = bufferSize,
                Id = id,
                Started = started,
                BindIPAddress = bindIPAddress,
                GroupId = groupId,
                MachineId = machineId,
                MachineName = machineName,
                Msg = msg,
                Port = port,
                TargetEP = targetEP,
                TargetMsg = targetMsg

            };
        }

        public SerializableForwardInfo(ForwardInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardInfoFormatter : MemoryPackFormatter<ForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<long>();
            value.Name = reader.ReadValue<string>();
            value.MachineId = reader.ReadValue<string>();
            value.GroupId = reader.ReadValue<string>();
            value.MachineName = reader.ReadValue<string>();
            value.BindIPAddress = reader.ReadValue<IPAddress>();
            value.Port = reader.ReadValue<int>();
            value.TargetEP = reader.ReadValue<IPEndPoint>();
            value.Started = reader.ReadValue<bool>();
            value.BufferSize = reader.ReadValue<byte>();
            value.Msg = reader.ReadValue<string>();
            value.TargetMsg = reader.ReadValue<string>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableForwardAddForwardInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardAddForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        ForwardInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableForwardAddForwardInfo(string machineId, ForwardInfo data)
        {
            this.info = new ForwardAddForwardInfo
            {
                MachineId = machineId,
                Data = data
            };
        }

        public SerializableForwardAddForwardInfo(ForwardAddForwardInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardAddForwardInfoFormatter : MemoryPackFormatter<ForwardAddForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardAddForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardAddForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardAddForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ForwardAddForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId =  reader.ReadValue<string>();
            value.Data = reader.ReadValue<ForwardInfo>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableForwardRemoveForwardInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardRemoveForwardInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        int Id => info.Id;

        [MemoryPackConstructor]
        SerializableForwardRemoveForwardInfo(string machineId, int id)
        {
            this.info = new ForwardRemoveForwardInfo
            {
                MachineId = machineId,
                Id = id
            };
        }

        public SerializableForwardRemoveForwardInfo(ForwardRemoveForwardInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardRemoveForwardInfoFormatter : MemoryPackFormatter<ForwardRemoveForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardRemoveForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardRemoveForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardRemoveForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ForwardRemoveForwardInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Id = reader.ReadValue<int>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableForwardCountInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardCountInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackConstructor]
        SerializableForwardCountInfo(string machineId, int count)
        {
            this.info = new ForwardCountInfo
            {
                MachineId = machineId,
                Count = count
            };
        }

        public SerializableForwardCountInfo(ForwardCountInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardCountInfoFormatter : MemoryPackFormatter<ForwardCountInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardCountInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardCountInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardCountInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value =new ForwardCountInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Count = reader.ReadValue<int>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableForwardTestInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardTestInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Target => info.Target;

        [MemoryPackInclude]
        string Msg => info.Msg;

        [MemoryPackConstructor]
        SerializableForwardTestInfo(IPEndPoint target, string msg)
        {
            this.info = new ForwardTestInfo
            {
                Target = target,
                Msg = msg
            };
        }

        public SerializableForwardTestInfo(ForwardTestInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardTestInfoFormatter : MemoryPackFormatter<ForwardTestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardTestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardTestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardTestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ForwardTestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Target = reader.ReadValue<IPEndPoint>();
            value.Msg = reader.ReadValue<string>();
        }
    }


}
