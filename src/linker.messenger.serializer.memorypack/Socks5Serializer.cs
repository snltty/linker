using MemoryPack;
using linker.messenger.socks5;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableSocks5LanInfo
    {
        [MemoryPackIgnore]
        public readonly Socks5LanInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress IP => info.IP;

        [MemoryPackInclude]
        byte PrefixLength => info.PrefixLength;

        [MemoryPackInclude]
        bool Disabled => info.Disabled;

        [MemoryPackInclude]
        bool Exists => info.Exists;

        [MemoryPackInclude]
        string Error => info.Error;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress MapIP => info.MapIP;

        [MemoryPackInclude]
        byte MapPrefixLength => info.MapPrefixLength;

        [MemoryPackInclude]
        string Remark => info.Remark;


        [MemoryPackConstructor]
        SerializableSocks5LanInfo(IPAddress ip, byte prefixLength, bool disabled, bool exists, string error, IPAddress mapip, byte mapprefixLength, string remark)
        {
            var info = new Socks5LanInfo
            {
                Disabled = disabled,
                Error = error,
                Exists = exists,
                IP = ip,
                PrefixLength = prefixLength,
                MapIP = mapip,
                MapPrefixLength = mapprefixLength,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableSocks5LanInfo(Socks5LanInfo info)
        {
            this.info = info;
        }
    }
    public class Socks5LanInfoFormatter : MemoryPackFormatter<Socks5LanInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5LanInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSocks5LanInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializableSocks5Info
    {
        [MemoryPackIgnore]
        public readonly Socks5Info info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        Socks5Status Status => info.Status;

        [MemoryPackInclude]
        int Port => info.Port;

        [MemoryPackInclude]
        List<Socks5LanInfo> Lans => info.Lans;

        [MemoryPackInclude]
        string SetupError => info.SetupError;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress Wan => info.Wan;


        [MemoryPackConstructor]
        SerializableSocks5Info(string machineId, Socks5Status status, int port, List<Socks5LanInfo> lans, string setupError, IPAddress wan)
        {
            var info = new Socks5Info { MachineId = machineId, Lans = lans, Port = port, SetupError = setupError, Status = status, Wan = wan };
            this.info = info;
        }

        public SerializableSocks5Info(Socks5Info info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableSocks5Info(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5Info value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSocks5Info>();
            value = wrapped.info;
        }
    }
}
