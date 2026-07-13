using MemoryPack;
using linker.messenger.api;
using System.Collections;

namespace linker.messenger.serializer.memorypack
{
    public class AccessUpdateInfoFormatter : MemoryPackFormatter<AccessUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }
            writer.WriteObjectHeader(3);
            writer.WriteValue(value.FromMachineId);
            writer.WriteValue(value.ToMachineId);
            writer.WriteValue(value.Access);
        }
        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }
            value = new AccessUpdateInfo();
            reader.TryReadObjectHeader(out byte count);
            value.FromMachineId = reader.ReadValue<string>();
            value.ToMachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<ulong>();
        }
    }
   
    public class AccessBitsUpdateInfoFormatter : MemoryPackFormatter<AccessBitsUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessBitsUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }
            writer.WriteObjectHeader(4);
            writer.WriteValue(value.FromMachineId);
            writer.WriteValue(value.ToMachineId);
            writer.WriteValue(value.Access);
            writer.WriteValue(value.FullAccess);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessBitsUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new AccessBitsUpdateInfo();
            reader.TryReadObjectHeader(out byte count);
            value.FromMachineId = reader.ReadValue<string>();
            value.ToMachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<BitArray>();
            if (count > 3)
            {
                value.FullAccess = reader.ReadValue<bool>();
            }
        }
    }

    public class AccessInfoFormatter : MemoryPackFormatter<AccessInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Access);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new AccessInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<AccessValue>();
        }
    }
   
    public class AccessBitsInfoFormatter : MemoryPackFormatter<AccessBitsInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref AccessBitsInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Access);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref AccessBitsInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new AccessBitsInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Access = reader.ReadValue<BitArray>();
        }
    }

    public class ApiPasswordUpdateInfoFormatter : MemoryPackFormatter<ApiPasswordUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ApiPasswordUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Password);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ApiPasswordUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new ApiPasswordUpdateInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Password = reader.ReadValue<string>();
        }
    }

}
