using linker.messenger.relay.client.transport;
using linker.messenger.relay.server;
using linker.tunnel.connection;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{


    [MemoryPackable]
    public readonly partial struct SerializableRelayTestInfo
    {
        [MemoryPackIgnore]
        public readonly RelayTestInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string SecretKey => string.Empty;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Server => info.Server;
        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackConstructor]
        SerializableRelayTestInfo(string machineId, string secretKey, IPEndPoint server, string userid)
        {
            var info = new RelayTestInfo { MachineId = machineId, Server = server, UserId = userid };
            this.info = info;
        }

        public SerializableRelayTestInfo(RelayTestInfo info)
        {
            this.info = info;
        }
    }
    public class RelayTestInfoFormatter : MemoryPackFormatter<RelayTestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayTestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayTestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayTestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayTestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            reader.ReadValue<string>(); // secret key
            value.Server = reader.ReadValue<IPEndPoint>();
            if (count > 3)
                value.UserId = reader.ReadValue<string>();

        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayInfo
    {
        [MemoryPackIgnore]
        public readonly RelayInfo info;

        [MemoryPackInclude]
        string FromMachineId => info.FromMachineId;
        [MemoryPackInclude]
        string FromMachineName => info.FromMachineName;
        [MemoryPackInclude]
        string RemoteMachineId => info.RemoteMachineId;
        [MemoryPackInclude]
        string RemoteMachineName => info.RemoteMachineName;
        [MemoryPackInclude]
        string TransactionId => info.TransactionId;
        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude]
        string TransportName => info.TransportName;
        [MemoryPackInclude]
        ulong FlowingId => info.FlowingId;
        [MemoryPackInclude]
        string NodeId => info.NodeId;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Server => info.Server;
        [MemoryPackInclude]
        bool SSL => info.SSL;

        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackConstructor]
        SerializableRelayInfo(string fromMachineId, string fromMachineName,
            string remoteMachineId, string remoteMachineName,
            string transactionId, string secretKey, string transportName, ulong flowingId,
            string nodeId, IPEndPoint server, bool ssl, string userid)
        {
            var info = new RelayInfo
            {
                FlowingId = flowingId,
                FromMachineId = fromMachineId,
                FromMachineName = fromMachineName,
                NodeId = nodeId,
                RemoteMachineId = remoteMachineId,
                RemoteMachineName = remoteMachineName,
                SSL = ssl,
                TransactionId = transactionId,
                TransportName = transportName,
                Server = server,
                UserId = userid,
            };
            this.info = info;
        }

        public SerializableRelayInfo(RelayInfo relayInfo)
        {
            this.info = relayInfo;
        }
    }
    public class RelayInfoFormatter : MemoryPackFormatter<RelayInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new RelayInfo();
            reader.TryReadObjectHeader(out byte count);
            value.FromMachineId = reader.ReadValue<string>();
            value.FromMachineName = reader.ReadValue<string>();
            value.RemoteMachineId = reader.ReadValue<string>();
            value.RemoteMachineName = reader.ReadValue<string>();
            value.TransactionId = reader.ReadValue<string>();
            reader.ReadValue<string>(); // secret key
            value.TransportName = reader.ReadValue<string>();
            value.FlowingId = reader.ReadValue<ulong>();
            value.NodeId = reader.ReadValue<string>();
            value.Server = reader.ReadValue<IPEndPoint>();
            value.SSL = reader.ReadValue<bool>();
            if (count > 11)
                value.UserId = reader.ReadValue<string>();
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableRelayAskResultInfo
    {
        [MemoryPackIgnore]
        public readonly RelayAskResultInfo info;

        [MemoryPackInclude]
        ulong FlowingId => info.FlowingId;
        [MemoryPackInclude]
        List<RelayServerNodeReportInfo> Nodes => info.Nodes;

        [MemoryPackConstructor]
        SerializableRelayAskResultInfo(ulong flowingId, List<RelayServerNodeReportInfo> nodes)
        {
            var info = new RelayAskResultInfo { FlowingId = flowingId, Nodes = nodes };
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

            var wrapped = reader.ReadPackable<SerializableRelayAskResultInfo>();
            value = wrapped.info;
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
        string NodeId => info.NodeId;

        [MemoryPackConstructor]
        SerializableRelayMessageInfo(RelayMessengerType type, ulong flowId, string fromId, string toId, string nodeId)
        {
            var info = new RelayMessageInfo
            {
                Type = type,
                FlowId = flowId,
                FromId = fromId,
                ToId = toId,
                NodeId = nodeId
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
                value.NodeId = reader.ReadValue<string>();
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeUpdateInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeUpdateInfo info;
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
        bool Public => info.Public;
        [MemoryPackInclude]
        string Url => info.Url;

        [MemoryPackInclude]
        bool AllowTcp => info.AllowTcp;
        [MemoryPackInclude]
        bool AllowUdp => info.AllowUdp;
        [MemoryPackInclude]
        bool Sync2Server => info.Sync2Server;

        [MemoryPackConstructor]
        SerializableRelayServerNodeUpdateInfo(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            bool Public, string url, bool allowTcp, bool allowUdp, bool sync2Server)
        {
            var info = new RelayServerNodeUpdateInfo
            {
                Id = id,
                MaxBandwidth = maxBandwidth,
                MaxBandwidthTotal = maxBandwidthTotal,
                MaxConnection = maxConnection,
                MaxGbTotal = maxGbTotal,
                MaxGbTotalLastBytes = maxGbTotalLastBytes,
                Name = name,
                Public = Public,
                Url = url,
                AllowTcp = allowTcp,
                AllowUdp = allowUdp,
                Sync2Server = sync2Server
            };
            this.info = info;
        }
        public SerializableRelayServerNodeUpdateInfo(RelayServerNodeUpdateInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeUpdateInfoFormatter : MemoryPackFormatter<RelayServerNodeUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeUpdateInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }
            value = new RelayServerNodeUpdateInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.MaxConnection = reader.ReadValue<int>();
            value.MaxBandwidth = reader.ReadValue<double>();
            value.MaxBandwidthTotal = reader.ReadValue<double>();
            value.MaxGbTotal = reader.ReadValue<double>();
            value.MaxGbTotalLastBytes = reader.ReadValue<long>();
            value.Public = reader.ReadValue<bool>();
            if (count > 8)
                value.Url = reader.ReadValue<string>();
            if (count > 9)
                value.AllowTcp = reader.ReadValue<bool>();
            if (count > 10)
                value.AllowUdp = reader.ReadValue<bool>();
            if (count > 11)
                value.Sync2Server = reader.ReadValue<bool>();
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeUpdateWrapInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeUpdateWrapInfo info;
        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        RelayServerNodeUpdateInfo Info => info.Info;

        [MemoryPackConstructor]
        SerializableRelayServerNodeUpdateWrapInfo(string secretKey, RelayServerNodeUpdateInfo info)
        {
            this.info = new RelayServerNodeUpdateWrapInfo
            {
                Info = info
            };
        }

        public SerializableRelayServerNodeUpdateWrapInfo(RelayServerNodeUpdateWrapInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeUpdateWrapInfoFormatter : MemoryPackFormatter<RelayServerNodeUpdateWrapInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeUpdateWrapInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeUpdateWrapInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeUpdateWrapInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerNodeUpdateWrapInfo>();
            value = wrapped.info;
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeReportInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeReportInfo info;

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
        SerializableRelayServerNodeReportInfo(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            double connectionRatio, double bandwidthRatio,
            bool Public, int delay,
            IPEndPoint endPoint, long lastTicks, string url, TunnelProtocolType allowProtocol, bool sync2Server, string version)
        {
            var info = new RelayServerNodeReportInfo
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
            if(count > 13)
                value.Url =  reader.ReadValue<string>();
            if (count > 14)
                value.AllowProtocol = reader.ReadValue<TunnelProtocolType>();
            if (count > 15)
                value.Sync2Server = reader.ReadValue<bool>();
            if (count > 16)
                value.Version = reader.ReadValue<string>();
        }
    }


}
