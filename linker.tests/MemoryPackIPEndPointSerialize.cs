
using linker.config;
using linker.plugins.serializes;
using linker.tunnel.connection;
using linker.tunnel.transport;
using MemoryPack;
using System.Net;

namespace linker.Tests
{
    [TestClass]
    public class MemoryPackIPEndPointSerialize
    {
        [TestMethod]
        public void Serialize()
        {
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter1());
            MemoryPackFormatterProvider.Register(new TunnelTransportWanPortInfoFormatter2());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter1());
            MemoryPackFormatterProvider.Register(new TunnelTransportInfoFormatter2());

            MemoryPackFormatterProvider.Register(new IPEndPointFormatter());
            MemoryPackFormatterProvider.Register(new IPAddressFormatter());


            TunnelTransportInfo2 tunnelTransportInfo2 = new TunnelTransportInfo2 { Local = new TunnelTransportWanPortInfo2 { MachineName = "local2" }, Remote = new TunnelTransportWanPortInfo2 { MachineName = "remote2" } };
            byte[] bytes = MemoryPackSerializer.Serialize(tunnelTransportInfo2);
            TunnelTransportInfo1 tunnelTransportInfo1 = MemoryPackSerializer.Deserialize<TunnelTransportInfo1>(bytes);

            Assert.AreEqual(tunnelTransportInfo2.Local.MachineName, tunnelTransportInfo1.Local.MachineName);
        }
    }


    public sealed partial class TunnelTransportWanPortInfo1
    {
        /// <summary>
        /// 我的本地
        /// </summary>
        public IPEndPoint Local { get; set; }
        /// <summary>
        /// 我的外网
        /// </summary>
        public IPEndPoint Remote { get; set; }
        /// <summary>
        /// 我的局域网IP
        /// </summary>
        public IPAddress[] LocalIps { get; set; }

        /// <summary>
        /// 我的外网层级
        /// </summary>
        public int RouteLevel { get; set; }

        /// <summary>
        /// 我的id
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 我的名称
        /// </summary>
        public string MachineName { get; set; }
    }
    public sealed partial class TunnelTransportWanPortInfo2
    {
        /// <summary>
        /// 我的本地
        /// </summary>
        public IPEndPoint Local { get; set; }
        /// <summary>
        /// 我的外网
        /// </summary>
        public IPEndPoint Remote { get; set; }
        /// <summary>
        /// 我的局域网IP
        /// </summary>
        public IPAddress[] LocalIps { get; set; }

        /// <summary>
        /// 我的外网层级
        /// </summary>
        public int RouteLevel { get; set; }

        /// <summary>
        /// 我的id
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 我的名称
        /// </summary>
        public string MachineName { get; set; }


        public int PortMapLan { get; set; }
        public int PortMapWan { get; set; }
    }

    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportWanPortInfo1
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportWanPortInfo1 tunnelTransportWanPortInfo;

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
        SerializableTunnelTransportWanPortInfo1(IPEndPoint local, IPEndPoint remote, IPAddress[] localIps, int routeLevel, string machineId, string machineName)
        {
            var tunnelTransportWanPortInfo = new TunnelTransportWanPortInfo1 { Local = local, Remote = remote, LocalIps = localIps, RouteLevel = routeLevel, MachineId = machineId, MachineName = machineName };
            this.tunnelTransportWanPortInfo = tunnelTransportWanPortInfo;
        }

        public SerializableTunnelTransportWanPortInfo1(TunnelTransportWanPortInfo1 tunnelTransportWanPortInfo)
        {
            this.tunnelTransportWanPortInfo = tunnelTransportWanPortInfo;
        }
    }
    public class TunnelTransportWanPortInfoFormatter1 : MemoryPackFormatter<TunnelTransportWanPortInfo1>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportWanPortInfo1 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportWanPortInfo1(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportWanPortInfo1 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportWanPortInfo1>();
            value = wrapped.tunnelTransportWanPortInfo;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportWanPortInfo2
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportWanPortInfo2 tunnelTransportWanPortInfo;

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
        int PortMapLan => tunnelTransportWanPortInfo.PortMapLan;

        [MemoryPackInclude]
        int PortMapWan => tunnelTransportWanPortInfo.PortMapWan;

        [MemoryPackConstructor]
        SerializableTunnelTransportWanPortInfo2(IPEndPoint local, IPEndPoint remote, IPAddress[] localIps, int routeLevel, string machineId, string machineName, int portMapLan, int portMapWan)
        {
            var tunnelTransportWanPortInfo = new TunnelTransportWanPortInfo2
            {
                Local = local,
                Remote = remote,
                LocalIps = localIps,
                RouteLevel = routeLevel,
                MachineId = machineId,
                MachineName = machineName,
                PortMapLan = portMapLan,
                PortMapWan = portMapWan
            };
            this.tunnelTransportWanPortInfo = tunnelTransportWanPortInfo;
        }

        public SerializableTunnelTransportWanPortInfo2(TunnelTransportWanPortInfo2 tunnelTransportWanPortInfo)
        {
            this.tunnelTransportWanPortInfo = tunnelTransportWanPortInfo;
        }
    }
    public class TunnelTransportWanPortInfoFormatter2 : MemoryPackFormatter<TunnelTransportWanPortInfo2>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportWanPortInfo2 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportWanPortInfo2(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportWanPortInfo2 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportWanPortInfo2>();
            value = wrapped.tunnelTransportWanPortInfo;
        }
    }


    public sealed partial class TunnelTransportInfo1
    {
        /// <summary>
        /// 我的
        /// </summary>
        public TunnelTransportWanPortInfo1 Local { get; set; }
        /// <summary>
        /// 对方的
        /// </summary>
        public TunnelTransportWanPortInfo1 Remote { get; set; }

        /// <summary>
        /// 事务
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// 协议类型
        /// </summary>
        public TunnelProtocolType TransportType { get; set; }
        /// <summary>
        /// 协议名
        /// </summary>
        public string TransportName { get; set; }
        /// <summary>
        /// 方向
        /// </summary>
        public TunnelDirection Direction { get; set; }
        /// <summary>
        /// 需要加密
        /// </summary>
        public bool SSL { get; set; }
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte BufferSize { get; set; } = 3;
        /// <summary>
        /// 目标ip列表
        /// </summary>
        public List<IPEndPoint> RemoteEndPoints { get; set; }
    }
    public sealed partial class TunnelTransportInfo2
    {
        /// <summary>
        /// 我的
        /// </summary>
        public TunnelTransportWanPortInfo2 Local { get; set; }
        /// <summary>
        /// 对方的
        /// </summary>
        public TunnelTransportWanPortInfo2 Remote { get; set; }

        /// <summary>
        /// 事务
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// 协议类型
        /// </summary>
        public TunnelProtocolType TransportType { get; set; }
        /// <summary>
        /// 协议名
        /// </summary>
        public string TransportName { get; set; }
        /// <summary>
        /// 方向
        /// </summary>
        public TunnelDirection Direction { get; set; }
        /// <summary>
        /// 需要加密
        /// </summary>
        public bool SSL { get; set; }
        /// <summary>
        /// 缓冲区
        /// </summary>
        public byte BufferSize { get; set; } = 3;
        /// <summary>
        /// 目标ip列表
        /// </summary>
        public List<IPEndPoint> RemoteEndPoints { get; set; }
    }
    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportInfo1
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportInfo1 tunnelTransportInfo;


        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelTransportWanPortInfo1 Local => tunnelTransportInfo.Local;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelTransportWanPortInfo1 Remote => tunnelTransportInfo.Remote;

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


        [MemoryPackConstructor]
        SerializableTunnelTransportInfo1(TunnelTransportWanPortInfo1 local, TunnelTransportWanPortInfo1 remote, string transactionId, TunnelProtocolType transportType, string transportName, TunnelDirection direction, bool ssl, byte bufferSize)
        {
            var tunnelTransportInfo = new TunnelTransportInfo1
            {
                Local = local,
                Remote = remote,
                TransactionId = transactionId,
                TransportName = transportName,
                TransportType = transportType,
                Direction = direction,
                SSL = ssl,
                BufferSize = bufferSize,
            };
            this.tunnelTransportInfo = tunnelTransportInfo;
        }

        public SerializableTunnelTransportInfo1(TunnelTransportInfo1 tunnelTransportInfo)
        {
            this.tunnelTransportInfo = tunnelTransportInfo;
        }
    }
    public class TunnelTransportInfoFormatter1 : MemoryPackFormatter<TunnelTransportInfo1>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportInfo1 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportInfo1(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportInfo1 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportInfo1>();
            value = wrapped.tunnelTransportInfo;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableTunnelTransportInfo2
    {
        [MemoryPackIgnore]
        public readonly TunnelTransportInfo2 tunnelTransportInfo;


        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelTransportWanPortInfo2 Local => tunnelTransportInfo.Local;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        TunnelTransportWanPortInfo2 Remote => tunnelTransportInfo.Remote;

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


        [MemoryPackConstructor]
        SerializableTunnelTransportInfo2(TunnelTransportWanPortInfo2 local, TunnelTransportWanPortInfo2 remote, string transactionId, TunnelProtocolType transportType, string transportName, TunnelDirection direction, bool ssl, byte bufferSize)
        {
            var tunnelTransportInfo = new TunnelTransportInfo2
            {
                Local = local,
                Remote = remote,
                TransactionId = transactionId,
                TransportName = transportName,
                TransportType = transportType,
                Direction = direction,
                SSL = ssl,
                BufferSize = bufferSize,
            };
            this.tunnelTransportInfo = tunnelTransportInfo;
        }

        public SerializableTunnelTransportInfo2(TunnelTransportInfo2 tunnelTransportInfo)
        {
            this.tunnelTransportInfo = tunnelTransportInfo;
        }
    }
    public class TunnelTransportInfoFormatter2 : MemoryPackFormatter<TunnelTransportInfo2>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelTransportInfo2 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelTransportInfo2(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelTransportInfo2 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableTunnelTransportInfo2>();
            value = wrapped.tunnelTransportInfo;
        }
    }
}