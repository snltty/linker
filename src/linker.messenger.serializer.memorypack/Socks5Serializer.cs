using MemoryPack;
using linker.messenger.socks5;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    public class Socks5LanInfoFormatter : MemoryPackFormatter<Socks5LanInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5LanInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(8);
            writer.WriteValue(value.IP);
            writer.WriteValue(value.PrefixLength);
            writer.WriteValue(value.Disabled);
            writer.WriteValue(value.Exists);
            writer.WriteValue(value.Error);
            writer.WriteValue(value.MapIP);
            writer.WriteValue(value.MapPrefixLength);
            writer.WriteValue(value.Remark);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5LanInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }
            reader.TryReadObjectHeader(out byte count);
            value = new Socks5LanInfo();
            value.IP = reader.ReadValue<IPAddress>();
            value.PrefixLength = reader.ReadValue<byte>();
            value.Disabled = reader.ReadValue<bool>();
            value.Exists = reader.ReadValue<bool>();
            value.Error = reader.ReadValue<string>();
            value.MapIP = reader.ReadValue<IPAddress>();
            value.MapPrefixLength = reader.ReadValue<byte>();
            if (count > 7)
                value.Remark = reader.ReadValue<string>();
        }
    }

    public class Socks5InfoFormatter : MemoryPackFormatter<Socks5Info>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5Info value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(6);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Status);
            writer.WriteValue(value.Port);
            writer.WriteValue(value.Lans);
            writer.WriteValue(value.SetupError);
            writer.WriteValue(value.Wan);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5Info value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new Socks5Info();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Status = reader.ReadValue<Socks5Status>();
            value.Port = reader.ReadValue<int>();
            value.Lans = reader.ReadValue<List<Socks5LanInfo>>();
            value.SetupError = reader.ReadValue<string>();
            value.Wan = reader.ReadValue<IPAddress>();
        }
    }
}
