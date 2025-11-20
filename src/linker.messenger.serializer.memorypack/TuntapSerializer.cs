using MemoryPack;
using linker.messenger.tuntap;
using System.Net;
using linker.messenger.tuntap.lease;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableTuntapVeaLanIPAddress
    {
        [MemoryPackIgnore]
        public readonly TuntapVeaLanIPAddress info;

        [MemoryPackInclude]
        uint IPAddress => info.IPAddress;

        [MemoryPackInclude]
        byte PrefixLength => info.PrefixLength;

        [MemoryPackInclude]
        uint MaskValue => info.MaskValue;

        [MemoryPackInclude]
        uint NetWork => info.NetWork;

        [MemoryPackInclude]
        uint Broadcast => info.Broadcast;

        [MemoryPackConstructor]
        SerializableTuntapVeaLanIPAddress(uint ipAddress, byte prefixLength, uint maskValue, uint netWork, uint broadcast)
        {
            var info = new TuntapVeaLanIPAddress
            {
                Broadcast = broadcast,
                IPAddress = ipAddress,
                PrefixLength = prefixLength,
                MaskValue = maskValue,
                NetWork = netWork,
            };
            this.info = info;
        }

        public SerializableTuntapVeaLanIPAddress(TuntapVeaLanIPAddress info)
        {
            this.info = info;
        }
    }
    public class TuntapVeaLanIPAddressFormatter : MemoryPackFormatter<TuntapVeaLanIPAddress>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapVeaLanIPAddress value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTuntapVeaLanIPAddress(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapVeaLanIPAddress value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTuntapVeaLanIPAddress>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableTuntapVeaLanIPAddressList
    {
        [MemoryPackIgnore]
        public readonly TuntapVeaLanIPAddressList info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        List<TuntapVeaLanIPAddress> IPS => info.IPS;

        [MemoryPackConstructor]
        SerializableTuntapVeaLanIPAddressList(string machineId, List<TuntapVeaLanIPAddress> ips)
        {
            var info = new TuntapVeaLanIPAddressList
            {
                IPS = ips,
                MachineId = machineId
            };
            this.info = info;
        }

        public SerializableTuntapVeaLanIPAddressList(TuntapVeaLanIPAddressList info)
        {
            this.info = info;
        }
    }
    public class TuntapVeaLanIPAddressListFormatter : MemoryPackFormatter<TuntapVeaLanIPAddressList>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapVeaLanIPAddressList value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTuntapVeaLanIPAddressList(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapVeaLanIPAddressList value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTuntapVeaLanIPAddressList>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableTuntapInfo
    {
        [MemoryPackIgnore]
        public readonly TuntapInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        TuntapStatus Status => info.Status;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress IP => info.IP;

        [MemoryPackInclude]
        byte PrefixLength => info.PrefixLength;
        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        List<TuntapLanInfo> Lans => info.Lans;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress Wan => info.Wan;

        [MemoryPackInclude]
        string SetupError => info.SetupError;

        [MemoryPackInclude]
        string NatError => info.NatError;

        [MemoryPackInclude]
        string SystemInfo => info.SystemInfo;

        [MemoryPackInclude]
        List<TuntapForwardInfo> Forwards => info.Forwards;

        [MemoryPackInclude]
        TuntapSwitch Switch => info.Switch;

        [MemoryPackConstructor]
        SerializableTuntapInfo(string machineId, TuntapStatus status, IPAddress ip, byte prefixLength, string name,
            List<TuntapLanInfo> lans, IPAddress wan, string setupError, string natError, string systemInfo, List<TuntapForwardInfo> forwards, TuntapSwitch Switch)
        {
            var info = new TuntapInfo
            {
                MachineId = machineId,
                Lans = lans,
                Wan = wan,
                Forwards = forwards,
                IP = ip,
                NatError = natError,
                SystemInfo = systemInfo,
                SetupError = setupError,
                PrefixLength = prefixLength,
                Name = name,
                Status = status,
                Switch = Switch,
            };
            this.info = info;
        }

        public SerializableTuntapInfo(TuntapInfo info)
        {
            this.info = info;
        }
    }
    public class TuntapInfoFormatter : MemoryPackFormatter<TuntapInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTuntapInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTuntapInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTuntapForwardInfo
    {
        [MemoryPackIgnore]
        public readonly TuntapForwardInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress ListenAddr => info.ListenAddr;

        [MemoryPackInclude]
        int ListenPort => info.ListenPort;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress ConnectAddr => info.ConnectAddr;

        [MemoryPackInclude]
        int ConnectPort => info.ConnectPort;

        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackConstructor]
        SerializableTuntapForwardInfo(IPAddress listenAddr, int listenPort, IPAddress connectAddr, int connectPort, string remark)
        {
            var info = new TuntapForwardInfo
            {
                ConnectAddr = connectAddr,
                ConnectPort = connectPort,
                ListenAddr = listenAddr,
                ListenPort = listenPort,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableTuntapForwardInfo(TuntapForwardInfo info)
        {
            this.info = info;
        }
    }
    public class TuntapForwardInfoFormatter : MemoryPackFormatter<TuntapForwardInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapForwardInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTuntapForwardInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapForwardInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTuntapForwardInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTuntapForwardTestWrapInfo
    {
        [MemoryPackIgnore]
        public readonly TuntapForwardTestWrapInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        List<TuntapForwardTestInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableTuntapForwardTestWrapInfo(string machineId, List<TuntapForwardTestInfo> list)
        {
            var info = new TuntapForwardTestWrapInfo
            {
                MachineId = machineId,
                List = list
            };
            this.info = info;
        }

        public SerializableTuntapForwardTestWrapInfo(TuntapForwardTestWrapInfo info)
        {
            this.info = info;
        }
    }
    public class TuntapForwardTestWrapInfoFormatter : MemoryPackFormatter<TuntapForwardTestWrapInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapForwardTestWrapInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTuntapForwardTestWrapInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapForwardTestWrapInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTuntapForwardTestWrapInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableTuntapForwardTestInfo
    {
        [MemoryPackIgnore]
        public readonly TuntapForwardTestInfo info;

        [MemoryPackInclude]
        int ListenPort => info.ListenPort;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress ConnectAddr => info.ConnectAddr;

        [MemoryPackInclude]
        int ConnectPort => info.ConnectPort;

        [MemoryPackInclude]
        string Error => info.Error;

        [MemoryPackConstructor]
        SerializableTuntapForwardTestInfo(int listenPort, IPAddress connectAddr, int connectPort, string error)
        {
            var info = new TuntapForwardTestInfo
            {
                ConnectAddr = connectAddr,
                ConnectPort = connectPort,
                ListenPort = listenPort,
                Error = error
            };
            this.info = info;
        }

        public SerializableTuntapForwardTestInfo(TuntapForwardTestInfo info)
        {
            this.info = info;
        }
    }
    public class TuntapForwardTestInfoFormatter : MemoryPackFormatter<TuntapForwardTestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapForwardTestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTuntapForwardTestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapForwardTestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTuntapForwardTestInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTuntapLanInfo
    {
        [MemoryPackIgnore]
        public readonly TuntapLanInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress IP => info.IP;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        byte PrefixLength => info.PrefixLength;

        [MemoryPackInclude]
        bool Disabled => info.Disabled;

        [MemoryPackInclude]
        bool Exists => info.Exists;

        [MemoryPackInclude]
        string Error => info.Error;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress MapIP => info.MapIP;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        byte MapPrefixLength => info.MapPrefixLength;

        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackConstructor]
        SerializableTuntapLanInfo(IPAddress ip, byte prefixLength, bool disabled, bool exists, string error, IPAddress mapip, byte mapprefixLength, string remark)
        {
            var info = new TuntapLanInfo
            {
                Disabled = disabled,
                Exists = exists,
                IP = ip,
                PrefixLength = prefixLength,
                Error = error,
                MapIP = mapip,
                MapPrefixLength = mapprefixLength,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableTuntapLanInfo(TuntapLanInfo info)
        {
            this.info = info;
        }
    }
    public class TuntapLanInfoFormatter : MemoryPackFormatter<TuntapLanInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TuntapLanInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTuntapLanInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TuntapLanInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            reader.TryReadObjectHeader(out byte count);
            value = new TuntapLanInfo();
            value.IP = reader.ReadValue<IPAddress>();
            value.PrefixLength = reader.ReadValue<byte>();
            value.Disabled = reader.ReadValue<bool>();
            value.Exists = reader.ReadValue<bool>();
            value.Error = reader.ReadValue<string>();
            value.MapIP = reader.ReadValue<IPAddress>();
            value.MapPrefixLength = reader.ReadValue<byte>();
            if (count > 7)
                value.Remark = reader.ReadValue<string>();

            //reader.Advance(count);
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableLeaseInfo
    {
        [MemoryPackIgnore]
        public readonly LeaseInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress IP => info.IP;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        byte PrefixLength => info.PrefixLength;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        string Name => info.Name;

        [MemoryPackConstructor]
        SerializableLeaseInfo(IPAddress ip, byte prefixLength, string name)
        {
            var info = new LeaseInfo
            {
                IP = ip,
                PrefixLength = prefixLength,
                Name = name
            };
            this.info = info;
        }

        public SerializableLeaseInfo(LeaseInfo info)
        {
            this.info = info;
        }
    }
    public class LeaseInfoFormatter : MemoryPackFormatter<LeaseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref LeaseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableLeaseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref LeaseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableLeaseInfo>();
            value = wrapped.info;
        }
    }
}
