using linker.messenger.relay.server;
using linker.tunnel.connection;
using linker.tunnel.transport;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{

    [MemoryPackable]
    public readonly partial struct SerializableRelayAskResultInfo
    {
        [MemoryPackIgnore]
        public readonly RelayAskResultInfo info;

        [MemoryPackInclude]
        string MasterId => info.MasterId;
        [MemoryPackInclude]
        List<RelayServerNodeStoreInfo> Nodes => info.Nodes;

        [MemoryPackConstructor]
        SerializableRelayAskResultInfo(string masterId, List<RelayServerNodeStoreInfo> nodes)
        {
            var info = new RelayAskResultInfo { MasterId = masterId, Nodes = nodes };
            this.info = info;
        }

        public SerializableRelayAskResultInfo(RelayAskResultInfo info)
        {
            this.info = info;
        }
    }
    public class RelayAskResultInfoFormatter : MemoryPackFormatter<RelayAskResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayAskResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayAskResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayAskResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }
            value = new RelayAskResultInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MasterId = reader.ReadValue<string>();
            if (count > 1)
                value.Nodes = reader.ReadValue<List<RelayServerNodeStoreInfo>>();
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableRelayCacheInfo
    {
        [MemoryPackIgnore]
        public readonly RelayCacheInfo info;

        [MemoryPackInclude]
        ulong FlowId => info.FlowId;
        [MemoryPackInclude]
        string FromId => info.FromId;
        [MemoryPackInclude]
        string FromName => info.FromName;
        [MemoryPackInclude]
        string ToId => info.ToId;
        [MemoryPackInclude]
        string ToName => info.ToName;
        [MemoryPackInclude]
        string GroupId => info.GroupId;

        [MemoryPackInclude]
        bool Super => info.Super;

        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;

        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackConstructor]
        SerializableRelayCacheInfo(ulong flowId, string fromId, string fromName, string toId, string toName, string groupId, bool super, double bandwidth, string userid)
        {
            var info = new RelayCacheInfo
            {
                FlowId = flowId,
                FromId = fromId,
                FromName = fromName,
                GroupId = groupId,
                ToId = toId,
                ToName = toName,
                Super = super,
                Bandwidth = bandwidth,
                UserId = userid
            };
            this.info = info;
        }

        public SerializableRelayCacheInfo(RelayCacheInfo info)
        {
            this.info = info;
        }
    }
    public class RelayCacheInfoFormatter : MemoryPackFormatter<RelayCacheInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayCacheInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayCacheInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayCacheInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayCacheInfo();
            reader.TryReadObjectHeader(out byte count);
            value.FlowId = reader.ReadValue<ulong>();
            value.FromId = reader.ReadValue<string>();
            value.FromName = reader.ReadValue<string>();
            value.ToId = reader.ReadValue<string>();
            value.ToName = reader.ReadValue<string>();
            value.GroupId = reader.ReadValue<string>();
            value.Super = reader.ReadValue<bool>();
            value.Bandwidth = reader.ReadValue<double>();
            if (count > 8)
                value.UserId = reader.ReadValue<string>();
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableRelayMessageInfo
    {
        [MemoryPackIgnore]
        public readonly RelayMessageInfo info;

        [MemoryPackInclude]
        RelayMessengerType Type => info.Type;
        [MemoryPackInclude]
        ulong FlowId => info.FlowId;
        [MemoryPackInclude]
        string FromId => info.FromId;
        [MemoryPackInclude]
        string ToId => info.ToId;
        [MemoryPackInclude]
        string MasterId => info.MasterId;

        [MemoryPackConstructor]
        SerializableRelayMessageInfo(RelayMessengerType type, ulong flowId, string fromId, string toId, string masterid)
        {
            var info = new RelayMessageInfo
            {
                Type = type,
                FlowId = flowId,
                FromId = fromId,
                ToId = toId,
                MasterId = masterid
            };
            this.info = info;
        }

        public SerializableRelayMessageInfo(RelayMessageInfo info)
        {
            this.info = info;
        }
    }
    public class RelayMessageInfoFormatter : MemoryPackFormatter<RelayMessageInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayMessageInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayMessageInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayMessageInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayMessageInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Type = reader.ReadValue<RelayMessengerType>();
            value.FlowId = reader.ReadValue<ulong>();
            value.FromId = reader.ReadValue<string>();
            value.ToId = reader.ReadValue<string>();
            if (count > 4)
                value.MasterId = reader.ReadValue<string>();
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeReportInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeReportInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string Host => info.Host;
        [MemoryPackInclude]
        TunnelProtocolType Protocol => info.Protocol;
        [MemoryPackInclude]
        int Connections => info.Connections;
        [MemoryPackInclude]
        int Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        int DataEachMonth => info.DataEachMonth;
        [MemoryPackInclude]
        long DataRemain => info.DataRemain;
        [MemoryPackInclude]
        string Url => info.Url;
        [MemoryPackInclude]
        string Logo => info.Logo;
        [MemoryPackInclude]
        string MasterKey => info.MasterKey;
        [MemoryPackInclude]
        string Version => info.Version;
        [MemoryPackInclude]
        int ConnectionsRatio => info.ConnectionsRatio;
        [MemoryPackInclude]
        double BandwidthRatio => info.BandwidthRatio;
        [MemoryPackInclude]
        int MasterCount => info.MasterCount;


        [MemoryPackConstructor]
        SerializableRelayServerNodeReportInfo(string nodeId, string name, string host, TunnelProtocolType protocol, int connections, int bandwidth, int dataEachMonth,
            long dataRemain, string url, string logo, string masterKey, string version, int connectionsRatio, double bandwidthRatio, int masterCount)
        {
            var info = new RelayServerNodeReportInfo
            {
                NodeId = nodeId,
                Name = name,
                Protocol = protocol,
                Connections = connections,
                Bandwidth = bandwidth,
                DataEachMonth = dataEachMonth,
                DataRemain = dataRemain,
                Url = url,
                Logo = logo,
                MasterKey = masterKey,
                Version = version,
                ConnectionsRatio = connectionsRatio,
                BandwidthRatio = bandwidthRatio,
                MasterCount = masterCount,
                Host = host
            };
            this.info = info;
        }

        public SerializableRelayServerNodeReportInfo(RelayServerNodeReportInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeReportInfoFormatter : MemoryPackFormatter<RelayServerNodeReportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeReportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeReportInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeReportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayServerNodeReportInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Host = reader.ReadValue<string>();
            value.Protocol = reader.ReadValue<TunnelProtocolType>();
            value.Connections = reader.ReadValue<int>();
            value.Bandwidth = reader.ReadValue<int>();
            value.DataEachMonth = reader.ReadValue<int>();
            value.DataRemain = reader.ReadValue<long>();
            value.Url = reader.ReadValue<string>();
            value.Logo = reader.ReadValue<string>();
            value.MasterKey = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            value.ConnectionsRatio = reader.ReadValue<int>();
            value.BandwidthRatio = reader.ReadValue<double>();
            value.MasterCount = reader.ReadValue<int>();

        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeStoreInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeStoreInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string Host => info.Host;
        [MemoryPackInclude]
        TunnelProtocolType Protocol => info.Protocol;
        [MemoryPackInclude]
        int Connections => info.Connections;
        [MemoryPackInclude]
        int Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        int DataEachMonth => info.DataEachMonth;
        [MemoryPackInclude]
        long DataRemain => info.DataRemain;
        [MemoryPackInclude]
        string Url => info.Url;
        [MemoryPackInclude]
        string Logo => info.Logo;
        [MemoryPackInclude]
        string MasterKey => info.MasterKey;
        [MemoryPackInclude]
        string Version => info.Version;

        [MemoryPackInclude]
        int ConnectionsRatio => info.ConnectionsRatio;
        [MemoryPackInclude]
        double BandwidthRatio => info.BandwidthRatio;
        [MemoryPackInclude]
        int MasterCount => info.MasterCount;


        [MemoryPackInclude]
        int Id => info.Id;
        [MemoryPackInclude]
        int BandwidthEachConnection => info.BandwidthEach;
        [MemoryPackInclude]
        bool Public => info.Public;
        [MemoryPackInclude]
        long LastTicks => info.LastTicks;

        [MemoryPackInclude]
        bool Manageable => info.Manageable;

        [MemoryPackConstructor]
        SerializableRelayServerNodeStoreInfo(string nodeId, string name, string host, TunnelProtocolType protocol, int connections, int bandwidth, int dataEachMonth,
            long dataRemain, string url, string logo, string masterKey, string version, int connectionsRatio, double bandwidthRatio, int masterCount,
           int id, int bandwidthEachConnection, bool Public, long lastTicks, bool manageable)
        {
            var info = new RelayServerNodeStoreInfo
            {
                NodeId = nodeId,
                Name = name,
                Protocol = protocol,
                Connections = connections,
                Bandwidth = bandwidth,
                DataEachMonth = dataEachMonth,
                DataRemain = dataRemain,
                Url = url,
                Logo = logo,
                MasterKey = masterKey,
                Version = version,
                ConnectionsRatio = connectionsRatio,
                BandwidthRatio = bandwidthRatio,
                MasterCount = masterCount,
                Id = id,
                Host = host,
                BandwidthEach = bandwidthEachConnection,
                Public = Public,
                LastTicks = lastTicks,
                Manageable = manageable
            };
            this.info = info;
        }

        public SerializableRelayServerNodeStoreInfo(RelayServerNodeStoreInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeStoreInfoFormatter : MemoryPackFormatter<RelayServerNodeStoreInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeStoreInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeStoreInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeStoreInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayServerNodeStoreInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Host = reader.ReadValue<string>();
            value.Protocol = reader.ReadValue<TunnelProtocolType>();
            value.Connections = reader.ReadValue<int>();
            value.Bandwidth = reader.ReadValue<int>();
            value.DataEachMonth = reader.ReadValue<int>();
            value.DataRemain = reader.ReadValue<long>();
            value.Url = reader.ReadValue<string>();
            value.Logo = reader.ReadValue<string>();
            value.MasterKey = reader.ReadValue<string>();
            value.Version = reader.ReadValue<string>();
            value.ConnectionsRatio = reader.ReadValue<int>();
            value.BandwidthRatio = reader.ReadValue<double>();
            value.MasterCount = reader.ReadValue<int>();
            value.Id = reader.ReadValue<int>();

            value.BandwidthEach = reader.ReadValue<int>();
            value.Public = reader.ReadValue<bool>();
            value.LastTicks = reader.ReadValue<long>();
            value.Manageable = reader.ReadValue<bool>();
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeReportInfoOld
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeReportInfoOld info;

        [MemoryPackInclude]
        string Id => info.Id;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        int MaxConnection => info.MaxConnection;
        [MemoryPackInclude]
        double MaxBandwidth => info.MaxBandwidth;
        [MemoryPackInclude]
        double MaxBandwidthTotal => info.MaxBandwidthTotal;
        [MemoryPackInclude]
        double MaxGbTotal => info.MaxGbTotal;
        [MemoryPackInclude]
        long MaxGbTotalLastBytes => info.MaxGbTotalLastBytes;
        [MemoryPackInclude]
        double ConnectionRatio => info.ConnectionRatio;
        [MemoryPackInclude]
        double BandwidthRatio => info.BandwidthRatio;
        [MemoryPackInclude]
        bool Public => info.Public;
        [MemoryPackInclude]
        int Delay => info.Delay;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint EndPoint => info.EndPoint;
        [MemoryPackInclude]
        long LastTicks => info.LastTicks;
        [MemoryPackInclude]
        string Url => info.Url;

        [MemoryPackInclude]
        TunnelProtocolType AllowProtocol => info.AllowProtocol;

        [MemoryPackInclude]
        bool Sync2Server => info.Sync2Server;
        [MemoryPackInclude]
        string Version => info.Version;


        [MemoryPackConstructor]
        SerializableRelayServerNodeReportInfoOld(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            double connectionRatio, double bandwidthRatio,
            bool Public, int delay,
            IPEndPoint endPoint, long lastTicks, string url, TunnelProtocolType allowProtocol, bool sync2Server, string version)
        {
            var info = new RelayServerNodeReportInfoOld
            {
                BandwidthRatio = bandwidthRatio,
                ConnectionRatio = connectionRatio,
                Delay = delay,
                EndPoint = endPoint,
                Id = id,
                LastTicks = lastTicks,
                MaxBandwidth = maxBandwidth,
                MaxBandwidthTotal = maxBandwidthTotal,
                MaxConnection = maxConnection,
                MaxGbTotal = maxGbTotal,
                MaxGbTotalLastBytes = maxGbTotalLastBytes,
                Name = name,
                Public = Public,
                Url = url,
                AllowProtocol = allowProtocol,
                Sync2Server = sync2Server,
                Version = version
            };
            this.info = info;
        }

        public SerializableRelayServerNodeReportInfoOld(RelayServerNodeReportInfoOld info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeReportInfoFormatterOld : MemoryPackFormatter<RelayServerNodeReportInfoOld>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeReportInfoOld value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeReportInfoOld(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeReportInfoOld value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayServerNodeReportInfoOld();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.MaxConnection = reader.ReadValue<int>();
            value.MaxBandwidth = reader.ReadValue<double>();
            value.MaxBandwidthTotal = reader.ReadValue<double>();
            value.MaxGbTotal = reader.ReadValue<double>();
            value.MaxGbTotalLastBytes = reader.ReadValue<long>();
            value.ConnectionRatio = reader.ReadValue<double>();
            value.BandwidthRatio = reader.ReadValue<double>();
            value.Public = reader.ReadValue<bool>();
            value.Delay = reader.ReadValue<int>();
            value.EndPoint = reader.ReadValue<IPEndPoint>();
            value.LastTicks = reader.ReadValue<long>();
            if (count > 13)
                value.Url = reader.ReadValue<string>();
            if (count > 14)
                value.AllowProtocol = reader.ReadValue<TunnelProtocolType>();
            if (count > 15)
                value.Sync2Server = reader.ReadValue<bool>();
            if (count > 16)
                value.Version = reader.ReadValue<string>();
        }
    }
}
