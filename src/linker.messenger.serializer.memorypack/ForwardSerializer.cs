using MemoryPack;
using System.Net;
using linker.messenger.forward;

namespace linker.messenger.serializer.memorypack
{
    public class ForwardInfoFormatter : MemoryPackFormatter<ForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(12);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.GroupId);
            writer.WriteValue(value.MachineName);
            writer.WriteValue(value.BindIPAddress);
            writer.WriteValue(value.Port);
            writer.WriteValue(value.TargetEP);
            writer.WriteValue(value.Started);
            writer.WriteValue(value.BufferSize);
            writer.WriteValue(value.Msg);
            writer.WriteValue(value.TargetMsg);
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

    public class ForwardAddForwardInfoFormatter : MemoryPackFormatter<ForwardAddForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardAddForwardInfo value)
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

    public class ForwardRemoveForwardInfoFormatter : MemoryPackFormatter<ForwardRemoveForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardRemoveForwardInfo value)
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

    public class ForwardCountInfoFormatter : MemoryPackFormatter<ForwardCountInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardCountInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Count);
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

    public class ForwardTestInfoFormatter : MemoryPackFormatter<ForwardTestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardTestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.Target);
            writer.WriteValue(value.Msg);
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
