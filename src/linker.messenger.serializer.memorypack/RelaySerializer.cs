using linker.messenger.relay.client.transport;
using linker.messenger.relay.server;
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
        string SecretKey => info.SecretKey;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Server => info.Server;
        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackConstructor]
        SerializableRelayTestInfo(string machineId, string secretKey, IPEndPoint server, string userid)
        {
            var info = new RelayTestInfo { MachineId = machineId, SecretKey = secretKey, Server = server, UserId = userid };
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
        string SecretKey => info.SecretKey;
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
                SecretKey = secretKey,
                Server = server,
                UserId = userid
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
        ulong MaxGbTotalLastBytes => info.MaxGbTotalLastBytes;
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
        List<string> UserIds => info.UserIds;


        [MemoryPackConstructor]
        SerializableRelayServerNodeReportInfo(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, ulong maxGbTotalLastBytes,
            double connectionRatio, double bandwidthRatio,
            bool Public, int delay,
            IPEndPoint endPoint, long lastTicks, List<string> userIds)
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
                UserIds = userIds
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
        bool Validated => info.Validated;


        [MemoryPackInclude, MemoryPackAllowSerialize]
        List<RelayServerCdkeyInfo> Cdkey => info.Cdkey;

        [MemoryPackConstructor]
        SerializableRelayCacheInfo(ulong flowId, string fromId, string fromName, string toId, string toName, string groupId, bool validated, List<RelayServerCdkeyInfo> cdkey)
        {
            var info = new RelayCacheInfo
            {
                FlowId = flowId,
                FromId = fromId,
                FromName = fromName,
                GroupId = groupId,
                ToId = toId,
                ToName = toName,
                Cdkey = cdkey,
                Validated = validated
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
    public readonly partial struct SerializableRelayServerCdkeyInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyInfo info;

        [MemoryPackInclude]
        string Id => info.Id;
        [MemoryPackInclude]
        string UserId => info.UserId;


        [MemoryPackInclude]
        string CdKey => info.CdKey;
        [MemoryPackInclude]
        DateTime AddTime => info.AddTime;
        [MemoryPackInclude]
        DateTime StartTime => info.StartTime;
        [MemoryPackInclude]
        DateTime EndTime => info.EndTime;
        [MemoryPackInclude]
        List<string> Nodes => info.Nodes;
        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        ulong MaxBytes => info.MaxBytes;
        [MemoryPackInclude]
        ulong LastBytes => info.LastBytes;
        [MemoryPackInclude]
        double Memory => info.Memory;
        [MemoryPackInclude]
        double PayMemory => info.PayMemory;
        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyInfo(string id, string userid, string cdKey, DateTime addTime, DateTime startTime, DateTime endTime,
            List<string> nodes, double bandwidth, ulong maxBytes, ulong lastBytes, double memory, double payMemory, string remark)
        {
            var info = new RelayServerCdkeyInfo
            {
                Id = id,
                UserId = userid,
                CdKey = cdKey,
                AddTime = addTime,
                StartTime = startTime,
                EndTime = endTime,
                Nodes = nodes,
                Bandwidth = bandwidth,
                MaxBytes = maxBytes,
                LastBytes = lastBytes,
                Memory = memory,
                PayMemory = payMemory,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyInfo(RelayServerCdkeyInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyInfoFormatter : MemoryPackFormatter<RelayServerCdkeyInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayServerCdkeyAddInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyAddInfo info;

        [MemoryPackInclude]
        string SecretKey => info.SecretKey;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        RelayServerCdkeyInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyAddInfo(string secretKey, RelayServerCdkeyInfo data)
        {
            var info = new RelayServerCdkeyAddInfo
            {
                SecretKey = secretKey,
                Data = data
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyAddInfo(RelayServerCdkeyAddInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyAddInfoFormatter : MemoryPackFormatter<RelayServerCdkeyAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyAddInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyAddInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayServerCdkeyDelInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyDelInfo info;

        [MemoryPackInclude]
        string SecretKey => info.SecretKey;
        [MemoryPackInclude]
        string Id => info.Id;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyDelInfo(string secretKey, string id)
        {
            var info = new RelayServerCdkeyDelInfo
            {
                SecretKey = secretKey,
                Id = id
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyDelInfo(RelayServerCdkeyDelInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyDelInfoFormatter : MemoryPackFormatter<RelayServerCdkeyDelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyDelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyDelInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyDelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyDelInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayServerCdkeyPageRequestInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyPageRequestInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        string Order => info.Order;
        [MemoryPackInclude]
        string Sort => info.Sort;

        [MemoryPackInclude]
        string UserId => info.UserId;
        [MemoryPackInclude]
        string Remark => info.Remark;
        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyPageRequestInfo(int page, int size, string order, string sort, string userid, string remark, string secretKey)
        {
            var info = new RelayServerCdkeyPageRequestInfo
            {
                Sort = sort,
                Order = order,
                Size = size,
                Page = page,
                UserId = userid,
                Remark = remark,
                SecretKey = secretKey
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyPageRequestInfo(RelayServerCdkeyPageRequestInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyPageRequestInfoFormatter : MemoryPackFormatter<RelayServerCdkeyPageRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyPageRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyPageRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyPageRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyPageRequestInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayServerCdkeyPageResultInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyPageResultInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<RelayServerCdkeyInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyPageResultInfo(int page, int size, int count, List<RelayServerCdkeyInfo> list)
        {
            var info = new RelayServerCdkeyPageResultInfo
            {
                Count = count,
                List = list,
                Size = size,
                Page = page
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyPageResultInfo(RelayServerCdkeyPageResultInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyPageResultInfoFormatter : MemoryPackFormatter<RelayServerCdkeyPageResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyPageResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyPageResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyPageResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyPageResultInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayTrafficReportInfo
    {
        [MemoryPackIgnore]
        public readonly RelayTrafficReportInfo info;

        [MemoryPackInclude]
        Dictionary<string, ulong> Id2Bytes => info.Id2Bytes;
        [MemoryPackInclude]
        List<string> UpdateIds => info.UpdateIds;
        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackConstructor]
        SerializableRelayTrafficReportInfo(Dictionary<string, ulong> id2Bytes, List<string> updateIds, string secretKey)
        {
            var info = new RelayTrafficReportInfo
            {
                Id2Bytes = id2Bytes,
                UpdateIds = updateIds,
                SecretKey = secretKey
            };
            this.info = info;
        }

        public SerializableRelayTrafficReportInfo(RelayTrafficReportInfo info)
        {
            this.info = info;
        }
    }
    public class RelayTrafficReportInfoFormatter : MemoryPackFormatter<RelayTrafficReportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayTrafficReportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayTrafficReportInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayTrafficReportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayTrafficReportInfo>();
            value = wrapped.info;
        }
    }


}
