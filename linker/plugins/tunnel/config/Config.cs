using Linker.Tunnel.Connection;
using Linker.Tunnel.WanPort;
using Linker.Tunnel.Transport;
using Linker.Tunnel.WanPort;
using LiteDB;
using MemoryPack;
using System.Net;
using System.Text.Json.Serialization;


namespace Linker.Client.Config
{
    public sealed partial class RunningConfigInfo
    {
        public TunnelRunningInfo Tunnel { get; set; } = new TunnelRunningInfo();
    }

    public sealed class TunnelRunningInfo
    {
        public ObjectId Id { get; set; }
        public TunnelWanPortInfo[] Servers { get; set; } = Array.Empty<TunnelWanPortInfo>();
        public int RouteLevelPlus { get; set; } = 0;

        public List<TunnelTransportItemInfo> Transports { get; set; } = new List<TunnelTransportItemInfo>();
    }
}

namespace Linker.Config
{
    public partial class ConfigClientInfo
    {
        [JsonIgnore]
        public TunnelConfigClientInfo Tunnel { get; set; } = new TunnelConfigClientInfo();
    }
    public sealed class TunnelConfigClientInfo
    {
        [JsonIgnore]
        public int RouteLevel { get; set; }

        [JsonIgnore]
        public IPAddress[] LocalIPs { get; set; }
    }


    [MemoryPackable]
    public sealed partial class TunnelTransportRouteLevelInfo
    {
        public string MachineId { get; set; }
        public int RouteLevel { get; set; } = 0;
        public int RouteLevelPlus { get; set; } = 0;
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelWanPortInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelWanPortInfo tunnelCompactInfo;

        [MemoryPackInclude]
        string Name => tunnelCompactInfo.Name;

        [MemoryPackInclude]
        TunnelWanPortType Type => tunnelCompactInfo.Type;

        [MemoryPackInclude]
        string Host => tunnelCompactInfo.Host;

        [MemoryPackInclude]
        bool Disabled => tunnelCompactInfo.Disabled;

        [MemoryPackConstructor]
        SerializableTunnelWanPortInfo(string name, TunnelWanPortType type, string host, bool disabled)
        {
            var tunnelCompactInfo = new TunnelWanPortInfo { Name = name, Type = type, Host = host, Disabled = disabled };
            this.tunnelCompactInfo = tunnelCompactInfo;
        }

        public SerializableTunnelWanPortInfo(TunnelWanPortInfo tunnelCompactInfo)
        {
            this.tunnelCompactInfo = tunnelCompactInfo;
        }
    }
    public class TunnelWanPortInfoFormatter : MemoryPackFormatter<TunnelWanPortInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelWanPortInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelWanPortInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelWanPortInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelWanPortInfo>();
            value = wrapped.tunnelCompactInfo;
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

        [MemoryPackConstructor]
        SerializableTunnelTransportWanPortInfo(IPEndPoint local, IPEndPoint remote, IPAddress[] localIps, int routeLevel, string machineId,string machineName)
        {
            var tunnelTransportWanPortInfo = new TunnelTransportWanPortInfo { Local = local, Remote = remote, LocalIps = localIps, RouteLevel = routeLevel, MachineId = machineId, MachineName= machineName };
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


        [MemoryPackConstructor]
        SerializableTunnelTransportItemInfo(string name, string label, string protocolType, bool disabled, bool reverse)
        {
            var tunnelTransportItemInfo = new TunnelTransportItemInfo { Name = name, Label = label, ProtocolType = protocolType, Disabled = disabled, Reverse = reverse };
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


        [MemoryPackConstructor]
        SerializableTunnelTransportInfo(TunnelTransportWanPortInfo local, TunnelTransportWanPortInfo remote, string transactionId, TunnelProtocolType transportType, string transportName, TunnelDirection direction, bool ssl)
        {
            var tunnelTransportInfo = new TunnelTransportInfo
            {
                Local = local,
                Remote = remote,
                TransactionId = transactionId,
                TransportName = transportName,
                TransportType = transportType,
                Direction = direction,
                SSL = ssl
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

            var wrapped = reader.ReadPackable<SerializableTunnelTransportInfo>();
            value = wrapped.tunnelTransportInfo;
        }
    }
}
