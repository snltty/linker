using cmonitor.tunnel.compact;
using cmonitor.tunnel.connection;
using cmonitor.tunnel.transport;
using common.libs;
using LiteDB;
using MemoryPack;
using System.Net;
using System.Text.Json.Serialization;


namespace cmonitor.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public TunnelRunningInfo Tunnel { get; set; } = new TunnelRunningInfo();
    }

    public sealed class TunnelRunningInfo
    {
        public ObjectId Id { get; set; }
        public TunnelCompactInfo[] Servers { get; set; } = Array.Empty<TunnelCompactInfo>();
        public int RouteLevelPlus { get; set; } = 0;

        public List<TunnelTransportItemInfo> Transports { get; set; } = new List<TunnelTransportItemInfo>();
    }
}

namespace cmonitor.config
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
        public string MachineName { get; set; }
        public int RouteLevel { get; set; } = 0;
        public int RouteLevelPlus { get; set; } = 0;
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelCompactInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelCompactInfo tunnelCompactInfo;

        [MemoryPackInclude]
        string Name => tunnelCompactInfo.Name;

        [MemoryPackInclude]
        TunnelCompactType Type => tunnelCompactInfo.Type;

        [MemoryPackInclude]
        string Host => tunnelCompactInfo.Host;

        [MemoryPackInclude]
        bool Disabled => tunnelCompactInfo.Disabled;

        [MemoryPackConstructor]
        SerializableTunnelCompactInfo(string name, TunnelCompactType type, string host, bool disabled)
        {
            var tunnelCompactInfo = new TunnelCompactInfo { Name = name, Type = type, Host = host, Disabled = disabled };
            this.tunnelCompactInfo = tunnelCompactInfo;
        }

        public SerializableTunnelCompactInfo(TunnelCompactInfo tunnelCompactInfo)
        {
            this.tunnelCompactInfo = tunnelCompactInfo;
        }
    }
    public class TunnelCompactInfoFormatter : MemoryPackFormatter<TunnelCompactInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelCompactInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelCompactInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelCompactInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelCompactInfo>();
            value = wrapped.tunnelCompactInfo;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportExternalIPInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportExternalIPInfo tunnelTransportExternalIPInfo;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Local => tunnelTransportExternalIPInfo.Local;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Remote => tunnelTransportExternalIPInfo.Remote;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPAddress[] LocalIps => tunnelTransportExternalIPInfo.LocalIps;

        [MemoryPackInclude]
        int RouteLevel => tunnelTransportExternalIPInfo.RouteLevel;

        [MemoryPackInclude]
        string MachineName => tunnelTransportExternalIPInfo.MachineName;

        [MemoryPackConstructor]
        SerializableTunnelTransportExternalIPInfo(IPEndPoint local, IPEndPoint remote, IPAddress[] localIps, int routeLevel, string machineName)
        {
            var tunnelTransportExternalIPInfo = new TunnelTransportExternalIPInfo { Local = local, Remote = remote, LocalIps = localIps, RouteLevel = routeLevel, MachineName = machineName };
            this.tunnelTransportExternalIPInfo = tunnelTransportExternalIPInfo;
        }

        public SerializableTunnelTransportExternalIPInfo(TunnelTransportExternalIPInfo tunnelTransportExternalIPInfo)
        {
            this.tunnelTransportExternalIPInfo = tunnelTransportExternalIPInfo;
        }
    }
    public class TunnelTransportExternalIPInfoFormatter : MemoryPackFormatter<TunnelTransportExternalIPInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportExternalIPInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportExternalIPInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportExternalIPInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportExternalIPInfo>();
            value = wrapped.tunnelTransportExternalIPInfo;
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
        TunnelTransportExternalIPInfo Local => tunnelTransportInfo.Local;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelTransportExternalIPInfo Remote => tunnelTransportInfo.Remote;

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
        SerializableTunnelTransportInfo(TunnelTransportExternalIPInfo local, TunnelTransportExternalIPInfo remote, string transactionId, TunnelProtocolType transportType, string transportName, TunnelDirection direction, bool ssl)
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
