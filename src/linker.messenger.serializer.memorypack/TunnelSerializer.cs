using linker.tunnel.connection;
using linker.tunnel.transport;
using linker.tunnel.wanport;
using System.Net;
using MemoryPack;
using linker.messenger.tunnel;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableTunnelWanPortProtocolInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelWanPortProtocolInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        TunnelWanPortProtocolType ProtocolType => info.ProtocolType;

        [MemoryPackConstructor]
        SerializableTunnelWanPortProtocolInfo(string machineId, TunnelWanPortProtocolType protocolType)
        {
            var info = new TunnelWanPortProtocolInfo { MachineId = machineId, ProtocolType = protocolType };
            this.info = info;
        }

        public SerializableTunnelWanPortProtocolInfo(TunnelWanPortProtocolInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class TunnelWanPortProtocolInfoFormatter : MemoryPackFormatter<TunnelWanPortProtocolInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelWanPortProtocolInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelWanPortProtocolInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelWanPortProtocolInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelWanPortProtocolInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportWanPortInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportWanPortInfo tunnelTransportWanPortInfo;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Local => tunnelTransportWanPortInfo.Local;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Remote => tunnelTransportWanPortInfo.Remote;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress[] LocalIps => tunnelTransportWanPortInfo.LocalIps;

        [MemoryPackInclude]
        int RouteLevel => tunnelTransportWanPortInfo.RouteLevel;

        [MemoryPackInclude]
        string MachineId => tunnelTransportWanPortInfo.MachineId;

        [MemoryPackInclude]
        string MachineName => tunnelTransportWanPortInfo.MachineName;

        [MemoryPackInclude]
        int PortMapWan => tunnelTransportWanPortInfo.PortMapWan;

        [MemoryPackInclude]
        int PortMapLan => tunnelTransportWanPortInfo.PortMapLan;

        [MemoryPackConstructor]
        SerializableTunnelTransportWanPortInfo(IPEndPoint local, IPEndPoint remote, IPAddress[] localIps, int routeLevel, string machineId, string machineName, int portMapWan, int portMapLan)
        {
            var tunnelTransportWanPortInfo = new TunnelTransportWanPortInfo
            {
                Local = local,
                Remote = remote,
                LocalIps = localIps,
                RouteLevel = routeLevel,
                MachineId = machineId,
                MachineName = machineName,
                PortMapWan = portMapWan,
                PortMapLan = portMapLan
            };
            this.tunnelTransportWanPortInfo = tunnelTransportWanPortInfo;
        }

        public SerializableTunnelTransportWanPortInfo(TunnelTransportWanPortInfo tunnelTransportWanPortInfo)
        {
            this.tunnelTransportWanPortInfo = tunnelTransportWanPortInfo;
        }
    }
    public class TunnelTransportWanPortInfoFormatter : MemoryPackFormatter<TunnelTransportWanPortInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportWanPortInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportWanPortInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportWanPortInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportWanPortInfo>();
            value = wrapped.tunnelTransportWanPortInfo;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportItemInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportItemInfo tunnelTransportItemInfo;


        [MemoryPackInclude]
        string Name => tunnelTransportItemInfo.Name;

        [MemoryPackInclude]
        string Label => tunnelTransportItemInfo.Label;

        [MemoryPackInclude]
        string ProtocolType => tunnelTransportItemInfo.ProtocolType;

        [MemoryPackInclude]
        bool Disabled => tunnelTransportItemInfo.Disabled;

        [MemoryPackInclude]
        bool Reverse => tunnelTransportItemInfo.Reverse;

        [MemoryPackInclude]
        bool SSL => tunnelTransportItemInfo.SSL;

        [MemoryPackInclude]
        byte BufferSize => tunnelTransportItemInfo.BufferSize;

        [MemoryPackInclude]
        byte Order => tunnelTransportItemInfo.Order;


        [MemoryPackConstructor]
        SerializableTunnelTransportItemInfo(string name, string label, string protocolType, bool disabled, bool reverse, bool ssl, byte buffersize, byte order)
        {
            var tunnelTransportItemInfo = new TunnelTransportItemInfo { Name = name, Label = label, ProtocolType = protocolType, Disabled = disabled, Reverse = reverse, SSL = ssl, BufferSize = buffersize, Order = order };
            this.tunnelTransportItemInfo = tunnelTransportItemInfo;
        }

        public SerializableTunnelTransportItemInfo(TunnelTransportItemInfo tunnelTransportItemInfo)
        {
            this.tunnelTransportItemInfo = tunnelTransportItemInfo;
        }
    }
    public class TunnelTransportItemInfoFormatter : MemoryPackFormatter<TunnelTransportItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportItemInfo>();
            value = wrapped.tunnelTransportItemInfo;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportInfo tunnelTransportInfo;


        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelTransportWanPortInfo Local => tunnelTransportInfo.Local;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelTransportWanPortInfo Remote => tunnelTransportInfo.Remote;

        [MemoryPackInclude]
        string TransactionId => tunnelTransportInfo.TransactionId;

        [MemoryPackInclude]
        TunnelProtocolType TransportType => tunnelTransportInfo.TransportType;

        [MemoryPackInclude]
        string TransportName => tunnelTransportInfo.TransportName;

        [MemoryPackInclude]
        TunnelDirection Direction => tunnelTransportInfo.Direction;

        [MemoryPackInclude]
        bool SSL => tunnelTransportInfo.SSL;

        [MemoryPackInclude]
        byte BufferSize => tunnelTransportInfo.BufferSize;

        [MemoryPackInclude]
        uint FlowId => tunnelTransportInfo.FlowId;

        [MemoryPackInclude]
        string TransactionTag => tunnelTransportInfo.TransactionTag;

        [MemoryPackConstructor]
        SerializableTunnelTransportInfo(TunnelTransportWanPortInfo local, TunnelTransportWanPortInfo remote, string transactionId,
            TunnelProtocolType transportType, string transportName, TunnelDirection direction, bool ssl, byte bufferSize, uint flowid, string transactionTag)
        {
            var tunnelTransportInfo = new TunnelTransportInfo
            {
                Local = local,
                Remote = remote,
                TransactionId = transactionId,
                TransportName = transportName,
                TransportType = transportType,
                Direction = direction,
                SSL = ssl,
                BufferSize = bufferSize,
                FlowId = flowid,
                TransactionTag = transactionTag
            };
            this.tunnelTransportInfo = tunnelTransportInfo;
        }

        public SerializableTunnelTransportInfo(TunnelTransportInfo tunnelTransportInfo)
        {
            this.tunnelTransportInfo = tunnelTransportInfo;
        }
    }
    public class TunnelTransportInfoFormatter : MemoryPackFormatter<TunnelTransportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelTransportInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Local = reader.ReadValue<TunnelTransportWanPortInfo>();
            value.Remote = reader.ReadValue<TunnelTransportWanPortInfo>();
            value.TransactionId = reader.ReadValue<string>();
            value.TransportType = reader.ReadValue<TunnelProtocolType>();
            value.TransportName = reader.ReadValue<string>();
            value.Direction = reader.ReadValue<TunnelDirection>();
            value.SSL = reader.ReadValue<bool>();
            value.BufferSize = reader.ReadValue<byte>();
            value.FlowId = reader.ReadValue<uint>();
            if (count > 9)
                value.TransactionTag = reader.ReadValue<string>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableTunnelRouteLevelInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelRouteLevelInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int RouteLevel => info.RouteLevel;
        [MemoryPackInclude]
        int RouteLevelPlus => info.RouteLevelPlus;

        [MemoryPackInclude]
        bool NeedReboot => info.NeedReboot;

        [MemoryPackInclude]
        int PortMapWan => info.PortMapWan;
        [MemoryPackInclude]
        int PortMapLan => info.PortMapLan;


        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelNetInfo Net => info.Net;

        [MemoryPackConstructor]
        SerializableTunnelRouteLevelInfo(string machineId, int routeLevel, int routeLevelPlus, bool needReboot, int portMapWan, int portMapLan, TunnelNetInfo net)
        {
            var info = new TunnelRouteLevelInfo
            {
                MachineId = machineId,
                NeedReboot = needReboot,
                PortMapWan = portMapWan,
                PortMapLan = portMapLan,
                RouteLevel = routeLevel,
                RouteLevelPlus = routeLevelPlus,
                Net = net
            };
            this.info = info;
        }

        public SerializableTunnelRouteLevelInfo(TunnelRouteLevelInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class TunnelRouteLevelInfoFormatter : MemoryPackFormatter<TunnelRouteLevelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelRouteLevelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelRouteLevelInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelRouteLevelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelRouteLevelInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelNetworkInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelLocalNetworkInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string HostName => info.HostName;
        [MemoryPackInclude]
        TunnelInterfaceInfo[] Lans => info.Lans;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress[] Routes => info.Routes;

        [MemoryPackConstructor]
        SerializableTunnelNetworkInfo(string machineId, string hostname, TunnelInterfaceInfo[] lans, IPAddress[] routes)
        {
            var info = new TunnelLocalNetworkInfo
            {
                MachineId = machineId,
                HostName = hostname,
                Lans = lans,
                Routes = routes,
            };
            this.info = info;
        }

        public SerializableTunnelNetworkInfo(TunnelLocalNetworkInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class TunnelNetworkInfoFormatter : MemoryPackFormatter<TunnelLocalNetworkInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelLocalNetworkInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelNetworkInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelLocalNetworkInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelNetworkInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelInterfaceInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelInterfaceInfo info;

        [MemoryPackInclude]
        string Name => info.Name;

        [MemoryPackInclude]
        string Desc => info.Desc;
        [MemoryPackInclude]
        string Mac => info.Mac;

        [MemoryPackInclude]
        IPAddress[] Ips => info.Ips;

        [MemoryPackConstructor]
        SerializableTunnelInterfaceInfo(string name, string desc, string mac, IPAddress[] ips)
        {
            var info = new TunnelInterfaceInfo { Name = name, Desc = desc, Mac = mac, Ips = ips };
            this.info = info;
        }

        public SerializableTunnelInterfaceInfo(TunnelInterfaceInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class TunnelInterfaceInfoFormatter : MemoryPackFormatter<TunnelInterfaceInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelInterfaceInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelInterfaceInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelInterfaceInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelInterfaceInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelNetInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelNetInfo info;
        [MemoryPackInclude]
        string CountryCode => info.CountryCode;
        [MemoryPackInclude]
        string City => info.City;
        [MemoryPackInclude]
        double Lat => info.Lat;
        [MemoryPackInclude]
        double Lon => info.Lon;
        [MemoryPackInclude]
        string Isp => info.Isp;
        [MemoryPackInclude]
        string Nat => info.Nat;

        [MemoryPackConstructor]
        SerializableTunnelNetInfo(string countryCode, string city, double lat, double lon, string isp, string nat)
        {
            var info = new TunnelNetInfo
            {
                City = city,
                CountryCode = countryCode,
                Isp = isp,
                Lat = lat,
                Lon = lon,
                Nat = nat
            };
            this.info = info;
        }

        public SerializableTunnelNetInfo(TunnelNetInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class TunnelNetInfoFormatter : MemoryPackFormatter<TunnelNetInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelNetInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelNetInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelNetInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelNetInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelSetRouteLevelInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelSetRouteLevelInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int RouteLevelPlus => info.RouteLevelPlus;

        [MemoryPackInclude]
        int PortMapWan => info.PortMapWan;
        [MemoryPackInclude]
        int PortMapLan => info.PortMapLan;

        [MemoryPackConstructor]
        SerializableTunnelSetRouteLevelInfo(string machineId, int routeLevelPlus, int portMapWan, int portMapLan)
        {
            var info = new TunnelSetRouteLevelInfo { MachineId = machineId, PortMapWan = portMapWan, PortMapLan = portMapLan, RouteLevelPlus = routeLevelPlus };
            this.info = info;
        }

        public SerializableTunnelSetRouteLevelInfo(TunnelSetRouteLevelInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class TunnelSetRouteLevelInfoFormatter : MemoryPackFormatter<TunnelSetRouteLevelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelSetRouteLevelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelSetRouteLevelInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelSetRouteLevelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelSetRouteLevelInfo>();
            value = wrapped.info;
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportItemSetInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportItemSetInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        List<TunnelTransportItemInfo> Data => info.Data;

        [MemoryPackConstructor]
        SerializableTunnelTransportItemSetInfo(string machineId, List<TunnelTransportItemInfo> data)
        {
            var info = new TunnelTransportItemSetInfo { MachineId = machineId, Data = data };
            this.info = info;
        }

        public SerializableTunnelTransportItemSetInfo(TunnelTransportItemSetInfo info)
        {
            this.info = info;
        }
    }
    public class TunnelTransportItemSetInfoFormatter : MemoryPackFormatter<TunnelTransportItemSetInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportItemSetInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportItemSetInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportItemSetInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportItemSetInfo>();
            value = wrapped.info;
        }
    }
}
