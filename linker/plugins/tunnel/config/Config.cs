using linker.tunnel.connection;
using linker.tunnel.wanport;
using linker.tunnel.transport;
using LiteDB;
using MemoryPack;
using System.Net;
using System.Text.Json.Serialization;
using linker.tunnel.adapter;


namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 打洞配置
        /// </summary>
        public TunnelRunningInfo Tunnel { get; set; } = new TunnelRunningInfo();
    }

    public sealed class TunnelRunningInfo
    {
        public ObjectId Id { get; set; }
        /// <summary>
        /// 外网端口服务器列表
        /// </summary>
        public List<TunnelWanPortInfo> Servers { get; set; } = new List<TunnelWanPortInfo>();
        /// <summary>
        /// 附加的网关层级
        /// </summary>
        public int RouteLevelPlus { get; set; }
        /// <summary>
        /// 打洞排除IP列表
        /// </summary>
        public ExcludeIPItem[] ExcludeIPs { get; set; } = Array.Empty<ExcludeIPItem>();

        /// <summary>
        /// 打洞协议列表
        /// </summary>
        public List<TunnelTransportItemInfo> Transports { get; set; } = new List<TunnelTransportItemInfo>();

        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }


        public IPAddress Interface { get; set; } = IPAddress.Any;
    }

    [MemoryPackable]
    public sealed partial class ExcludeIPItem
    {
        [MemoryPackAllowSerialize]
        public IPAddress IPAddress { get; set; }
        public byte Mask { get; set; } = 32;
    }
}

namespace linker.config
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

        [JsonIgnore]
        public IPAddress[] RouteIPs { get; set; }
    }


    [MemoryPackable]
    public sealed partial class TunnelTransportRouteLevelInfo
    {
        public string MachineId { get; set; }
        public int RouteLevel { get; set; }
        public int RouteLevelPlus { get; set; }

        public bool NeedReboot { get; set; }

        public int PortMapWan { get; set; }
        public int PortMapLan { get; set; }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelWanPortProtocolInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelWanPortProtocolInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        TunnelWanPortType Type => info.Type;

        [MemoryPackInclude]
        TunnelWanPortProtocolType ProtocolType => info.ProtocolType;

        [MemoryPackConstructor]
        SerializableTunnelWanPortProtocolInfo(string machineId, TunnelWanPortType type, TunnelWanPortProtocolType protocolType)
        {
            var info = new TunnelWanPortProtocolInfo { MachineId = machineId, Type = type, ProtocolType = protocolType };
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
    public readonly partial struct SerializableTunnelWanPortInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelWanPortInfo tunnelCompactInfo;

        [MemoryPackInclude]
        string Name => tunnelCompactInfo.Name;

        [MemoryPackInclude]
        TunnelWanPortType Type => tunnelCompactInfo.Type;

        [MemoryPackInclude]
        TunnelWanPortProtocolType ProtocolType => tunnelCompactInfo.ProtocolType;

        [MemoryPackInclude]
        string Host => tunnelCompactInfo.Host;

        [MemoryPackInclude]
        bool Disabled => tunnelCompactInfo.Disabled;

        [MemoryPackConstructor]
        SerializableTunnelWanPortInfo(string name, TunnelWanPortType type, TunnelWanPortProtocolType protocolType, string host, bool disabled)
        {
            var tunnelCompactInfo = new TunnelWanPortInfo { Name = name, Type = type, ProtocolType = protocolType, Host = host, Disabled = disabled };
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


        [MemoryPackConstructor]
        SerializableTunnelTransportInfo(TunnelTransportWanPortInfo local, TunnelTransportWanPortInfo remote, string transactionId, TunnelProtocolType transportType, string transportName, TunnelDirection direction, bool ssl, byte bufferSize, uint flowid)
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
                FlowId = flowid
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
