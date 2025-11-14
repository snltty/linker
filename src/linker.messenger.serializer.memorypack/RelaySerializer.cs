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
        // 【安全修复 P0】SecretKey 现在正确存储和返回，而不是总是返回空字符串
        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Server => info.Server;

        [MemoryPackConstructor]
        // 【安全修复】正确存储 secretKey 参数
        SerializableRelayTestInfo(string machineId, string secretKey, IPEndPoint server)
        {
            var info = new RelayTestInfo { MachineId = machineId, Server = server, SecretKey = secretKey };
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

            var wrapped = reader.ReadPackable<SerializableRelayTestInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayTestInfo170
    {
        [MemoryPackIgnore]
        public readonly RelayTestInfo170 info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        // 【安全修复 P0】SecretKey 现在正确存储和返回
        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Server => info.Server;
        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackConstructor]
        // 【安全修复】正确存储 secretKey 参数
        SerializableRelayTestInfo170(string machineId, string secretKey, IPEndPoint server, string userid)
        {
            var info = new RelayTestInfo170 { MachineId = machineId, Server = server, UserId = userid, SecretKey = secretKey };
            this.info = info;
        }

        public SerializableRelayTestInfo170(RelayTestInfo170 info)
        {
            this.info = info;
        }
    }
    public class RelayTestInfo170Formatter : MemoryPackFormatter<RelayTestInfo170>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayTestInfo170 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayTestInfo170(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayTestInfo170 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayTestInfo170>();
            value = wrapped.info;
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


        [MemoryPackConstructor]
        SerializableRelayInfo(string fromMachineId, string fromMachineName,
            string remoteMachineId, string remoteMachineName,
            string transactionId, string secretKey, string transportName, ulong flowingId,
            string nodeId, IPEndPoint server, bool ssl)
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
                Server = server
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

            var wrapped = reader.ReadPackable<SerializableRelayInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayInfo170
    {
        [MemoryPackIgnore]
        public readonly RelayInfo170 info;

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

        [MemoryPackInclude]
        bool UseCdkey => info.UseCdkey;


        [MemoryPackConstructor]
        SerializableRelayInfo170(string fromMachineId, string fromMachineName,
            string remoteMachineId, string remoteMachineName,
            string transactionId, string secretKey, string transportName, ulong flowingId,
            string nodeId, IPEndPoint server, bool ssl, string userid, bool useCdkey)
        {
            var info = new RelayInfo170
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
                UseCdkey = useCdkey
            };
            this.info = info;
        }

        public SerializableRelayInfo170(RelayInfo170 relayInfo)
        {
            this.info = relayInfo;
        }
    }
    public class RelayInfo170Formatter : MemoryPackFormatter<RelayInfo170>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayInfo170 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayInfo170(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayInfo170 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayInfo170>();
            value = wrapped.info;
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

        [MemoryPackConstructor]
        SerializableRelayServerNodeUpdateInfo(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            bool Public, string url, bool allowTcp, bool allowUdp)
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

            var wrapped = reader.ReadPackable<SerializableRelayServerNodeUpdateInfo>();
            value = wrapped.info;
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


        [MemoryPackConstructor]
        SerializableRelayServerNodeReportInfo(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            double connectionRatio, double bandwidthRatio,
            bool Public, int delay,
            IPEndPoint endPoint, long lastTicks)
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
                Public = Public
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

            var wrapped = reader.ReadPackable<SerializableRelayServerNodeReportInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeReportInfo170
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeReportInfo170 info;

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


        [MemoryPackConstructor]
        SerializableRelayServerNodeReportInfo170(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            double connectionRatio, double bandwidthRatio,
            bool Public, int delay,
            IPEndPoint endPoint, long lastTicks, string url, TunnelProtocolType allowProtocol)
        {
            var info = new RelayServerNodeReportInfo170
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
                AllowProtocol = allowProtocol
            };
            this.info = info;
        }

        public SerializableRelayServerNodeReportInfo170(RelayServerNodeReportInfo170 info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeReportInfo170Formatter : MemoryPackFormatter<RelayServerNodeReportInfo170>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeReportInfo170 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeReportInfo170(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeReportInfo170 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerNodeReportInfo170>();
            value = wrapped.info;
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
    public readonly partial struct SerializableRelayAskResultInfo170
    {
        [MemoryPackIgnore]
        public readonly RelayAskResultInfo170 info;

        [MemoryPackInclude]
        ulong FlowingId => info.FlowingId;
        [MemoryPackInclude]
        List<RelayServerNodeReportInfo170> Nodes => info.Nodes;

        [MemoryPackConstructor]
        SerializableRelayAskResultInfo170(ulong flowingId, List<RelayServerNodeReportInfo170> nodes)
        {
            var info = new RelayAskResultInfo170 { FlowingId = flowingId, Nodes = nodes };
            this.info = info;
        }

        public SerializableRelayAskResultInfo170(RelayAskResultInfo170 info)
        {
            this.info = info;
        }
    }
    public class RelayAskResultInfo170Formatter : MemoryPackFormatter<RelayAskResultInfo170>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayAskResultInfo170 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayAskResultInfo170(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayAskResultInfo170 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayAskResultInfo170>();
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
        SerializableRelayCacheInfo(ulong flowId, string fromId, string fromName, string toId, string toName, string groupId, bool super, double bandwidth,string userid)
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

            var wrapped = reader.ReadPackable<SerializableRelayCacheInfo>();
            value = wrapped.info;
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

            var wrapped = reader.ReadPackable<SerializableRelayMessageInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayTestInfo188
    {
        [MemoryPackIgnore]
        public readonly RelayTestInfo188 info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string SecretKey => string.Empty;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Server => info.Server;
        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackConstructor]
        SerializableRelayTestInfo188(string machineId, string secretKey, IPEndPoint server, string userid)
        {
            var info = new RelayTestInfo188 { MachineId = machineId, Server = server, UserId = userid };
            this.info = info;
        }

        public SerializableRelayTestInfo188(RelayTestInfo188 info)
        {
            this.info = info;
        }
    }
    public class RelayTestInfo188Formatter : MemoryPackFormatter<RelayTestInfo188>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayTestInfo188 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayTestInfo188(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayTestInfo188 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayTestInfo188>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeUpdateInfo188
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeUpdateInfo188 info;
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
        SerializableRelayServerNodeUpdateInfo188(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            bool Public, string url, bool allowTcp, bool allowUdp, bool sync2Server)
        {
            var info = new RelayServerNodeUpdateInfo188
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

        public SerializableRelayServerNodeUpdateInfo188(RelayServerNodeUpdateInfo188 info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeUpdateInfo188Formatter : MemoryPackFormatter<RelayServerNodeUpdateInfo188>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeUpdateInfo188 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeUpdateInfo188(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeUpdateInfo188 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerNodeUpdateInfo188>();
            value = wrapped.info;
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeUpdateWrapInfo188
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeUpdateWrapInfo188 info;
        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        RelayServerNodeUpdateInfo188 Info => info.Info;

        [MemoryPackConstructor]
        SerializableRelayServerNodeUpdateWrapInfo188(string secretKey, RelayServerNodeUpdateInfo188 info)
        {
            this.info = new RelayServerNodeUpdateWrapInfo188
            {
                Info = info
            };
        }

        public SerializableRelayServerNodeUpdateWrapInfo188(RelayServerNodeUpdateWrapInfo188 info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeUpdateWrapInfo188Formatter : MemoryPackFormatter<RelayServerNodeUpdateWrapInfo188>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeUpdateWrapInfo188 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeUpdateWrapInfo188(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeUpdateWrapInfo188 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerNodeUpdateWrapInfo188>();
            value = wrapped.info;
        }
    }
    [MemoryPackable]
    public readonly partial struct SerializableRelayServerNodeReportInfo188
    {
        [MemoryPackIgnore]
        public readonly RelayServerNodeReportInfo188 info;

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
        SerializableRelayServerNodeReportInfo188(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            double connectionRatio, double bandwidthRatio,
            bool Public, int delay,
            IPEndPoint endPoint, long lastTicks, string url, TunnelProtocolType allowProtocol, bool sync2Server, string version)
        {
            var info = new RelayServerNodeReportInfo188
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

        public SerializableRelayServerNodeReportInfo188(RelayServerNodeReportInfo188 info)
        {
            this.info = info;
        }
    }
    public class RelayServerNodeReportInfo188Formatter : MemoryPackFormatter<RelayServerNodeReportInfo188>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerNodeReportInfo188 value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerNodeReportInfo188(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerNodeReportInfo188 value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerNodeReportInfo188>();
            value = wrapped.info;
        }
    }


}
