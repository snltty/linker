using linker.messenger.relay.client.transport;
using linker.messenger.relay.server;
using MemoryPack;
using System.Net;
using System.Xml.Linq;

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

        [MemoryPackConstructor]
        SerializableRelayTestInfo(string machineId, string secretKey, IPEndPoint server)
        {
            var info = new RelayTestInfo { MachineId = machineId, SecretKey = secretKey, Server = server };
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
        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Server => info.Server;
        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackConstructor]
        SerializableRelayTestInfo170(string machineId, string secretKey, IPEndPoint server, string userid)
        {
            var info = new RelayTestInfo170 { MachineId = machineId, SecretKey = secretKey, Server = server, UserId = userid };
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
                SecretKey = secretKey,
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
        SerializableRelayInfo170(string fromMachineId, string fromMachineName,
            string remoteMachineId, string remoteMachineName,
            string transactionId, string secretKey, string transportName, ulong flowingId,
            string nodeId, IPEndPoint server, bool ssl, string userid)
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
                SecretKey = secretKey,
                Server = server,
                UserId = userid
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

        [MemoryPackConstructor]
        SerializableRelayServerNodeUpdateInfo(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            bool Public, string url)
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
                Url = url
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
        string SecretKey => info.SecretKey;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        RelayServerNodeUpdateInfo Info => info.Info;

        [MemoryPackConstructor]
        SerializableRelayServerNodeUpdateWrapInfo(
             string secretKey, RelayServerNodeUpdateInfo info)
        {
            this.info = new RelayServerNodeUpdateWrapInfo
            {
                SecretKey = secretKey,
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


        [MemoryPackConstructor]
        SerializableRelayServerNodeReportInfo(
            string id, string name,
            int maxConnection, double maxBandwidth, double maxBandwidthTotal,
            double maxGbTotal, long maxGbTotalLastBytes,
            double connectionRatio, double bandwidthRatio,
            bool Public, int delay,
            IPEndPoint endPoint, long lastTicks, string url)
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
                Url = url
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
        long CdkeyId => info.CdkeyId;

        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        long LastBytes => info.LastBytes;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyInfo(long cdkeyid, double bandwidth, long lastBytes)
        {
            var info = new RelayServerCdkeyInfo
            {
                CdkeyId = cdkeyid,
                Bandwidth = bandwidth,
                LastBytes = lastBytes
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
    public readonly partial struct SerializableRelayServerCdkeyStoreInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyStoreInfo info;

        [MemoryPackInclude]
        long CdkeyId => info.CdkeyId;
        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        long LastBytes => info.LastBytes;

        [MemoryPackInclude]
        string Id => info.Id;
        [MemoryPackInclude]
        string UserId => info.UserId;

        [MemoryPackInclude]
        DateTime AddTime => info.AddTime;
        [MemoryPackInclude]
        DateTime StartTime => info.StartTime;
        [MemoryPackInclude]
        DateTime EndTime => info.EndTime;
        [MemoryPackInclude]
        DateTime UseTime => info.UseTime;

        [MemoryPackInclude]
        long MaxBytes => info.MaxBytes;

        [MemoryPackInclude]
        double CostPrice => info.CostPrice;
        [MemoryPackInclude]
        double Price => info.Price;
        [MemoryPackInclude]
        double UserPrice => info.UserPrice;
        [MemoryPackInclude]
        double PayPrice => info.PayPrice;
        [MemoryPackInclude]
        string Remark => info.Remark;
        [MemoryPackInclude]
        string OrderId => info.OrderId;
        [MemoryPackInclude]
        string Contact => info.Contact;
        [MemoryPackInclude]
        bool Deleted => info.Deleted;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyStoreInfo(long cdkeyid, double bandwidth, long lastBytes, string id, string userid, DateTime addTime, DateTime startTime, DateTime endTime, DateTime useTime, long maxBytes, double costPrice, double price, double userPrice, double payPrice, string remark, string orderId, string contact, bool deleted)
        {
            var info = new RelayServerCdkeyStoreInfo
            {
                CdkeyId = cdkeyid,
                Bandwidth = bandwidth,
                LastBytes = lastBytes,
                Id = id,
                UserId = userid,
                AddTime = addTime,
                StartTime = startTime,
                EndTime = endTime,
                UseTime = useTime,
                MaxBytes = maxBytes,
                CostPrice = costPrice,
                Price = price,
                UserPrice = userPrice,
                PayPrice = payPrice,
                Remark = remark,
                OrderId = orderId,
                Contact = contact,
                Deleted = deleted
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyStoreInfo(RelayServerCdkeyStoreInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyStoreInfoFormatter : MemoryPackFormatter<RelayServerCdkeyStoreInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyStoreInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyStoreInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyStoreInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyStoreInfo>();
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
        RelayServerCdkeyStoreInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyAddInfo(string secretKey, RelayServerCdkeyStoreInfo data)
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
        string UserId => info.UserId;
        [MemoryPackInclude]
        long CdkeyId => info.CdkeyId;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyDelInfo(string secretKey, string userid, long cdkeyid)
        {
            var info = new RelayServerCdkeyDelInfo
            {
                SecretKey = secretKey,
                UserId = userid,
                CdkeyId = cdkeyid
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
        string OrderId => info.OrderId;
        [MemoryPackInclude]
        string Contact => info.Contact;
        [MemoryPackInclude]
        string SecretKey => info.SecretKey;
        [MemoryPackInclude]
        RelayServerCdkeyPageRequestFlag Flag => info.Flag;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyPageRequestInfo(int page, int size, string order, string sort, string userid, string remark, string orderid, string contact, string secretKey, RelayServerCdkeyPageRequestFlag flag)
        {
            var info = new RelayServerCdkeyPageRequestInfo
            {
                Sort = sort,
                Order = order,
                Size = size,
                Page = page,
                UserId = userid,
                Remark = remark,
                OrderId = orderid,
                Contact = contact,
                SecretKey = secretKey,
                Flag = flag
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
        List<RelayServerCdkeyStoreInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyPageResultInfo(int page, int size, int count, List<RelayServerCdkeyStoreInfo> list)
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
    public readonly partial struct SerializableRelayTrafficUpdateInfo
    {
        [MemoryPackIgnore]
        public readonly RelayTrafficUpdateInfo info;

        [MemoryPackInclude]
        Dictionary<long, long> Dic => info.Dic;
        [MemoryPackInclude]
        List<long> Ids => info.Ids;
        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackConstructor]
        SerializableRelayTrafficUpdateInfo(Dictionary<long, long> dic, List<long> ids, string secretKey)
        {
            var info = new RelayTrafficUpdateInfo
            {
                Dic = dic,
                Ids = ids,
                SecretKey = secretKey
            };
            this.info = info;
        }

        public SerializableRelayTrafficUpdateInfo(RelayTrafficUpdateInfo info)
        {
            this.info = info;
        }
    }
    public class RelayTrafficUpdateInfoFormatter : MemoryPackFormatter<RelayTrafficUpdateInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayTrafficUpdateInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayTrafficUpdateInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayTrafficUpdateInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayTrafficUpdateInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayServerCdkeyImportInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyImportInfo info;

        [MemoryPackInclude]
        string SecretKey => info.SecretKey;
        [MemoryPackInclude]
        string UserId => info.UserId;
        [MemoryPackInclude]
        string Base64 => info.Base64;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyImportInfo(string secretKey, string userid, string base64)
        {
            var info = new RelayServerCdkeyImportInfo
            {
                SecretKey = secretKey,
                UserId = userid,
                Base64 = base64
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyImportInfo(RelayServerCdkeyImportInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyImportInfoFormatter : MemoryPackFormatter<RelayServerCdkeyImportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyImportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyImportInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyImportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyImportInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableRelayServerCdkeyTestResultInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyTestResultInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        RelayServerCdkeyOrderInfo Order => info.Order;
        [MemoryPackInclude]
        string Cdkey => info.Cdkey;
        [MemoryPackInclude]
        List<string> Field => info.Field;

        [MemoryPackConstructor]
        SerializableRelayServerCdkeyTestResultInfo(RelayServerCdkeyOrderInfo order, string cdkey, List<string> field)
        {
            var info = new RelayServerCdkeyTestResultInfo
            {
                Order = order,
                Cdkey = cdkey,
                Field = field
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyTestResultInfo(RelayServerCdkeyTestResultInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyTestResultInfoFormatter : MemoryPackFormatter<RelayServerCdkeyTestResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyTestResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyTestResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyTestResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyTestResultInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableRelayServerCdkeyOrderInfo
    {
        [MemoryPackIgnore]
        public readonly RelayServerCdkeyOrderInfo info;

        [MemoryPackInclude]
        int GB => info.GB;
        [MemoryPackInclude]
        int Speed => info.Speed;
        [MemoryPackInclude]
        string Time => info.Time;
        [MemoryPackInclude]
        string WidgetUserId => info.WidgetUserId;
        [MemoryPackInclude]
        string OrderId => info.OrderId;
        [MemoryPackInclude]
        string Contact => info.Contact;

        [MemoryPackInclude]
        double CostPrice => info.CostPrice;
        [MemoryPackInclude]
        double Price => info.Price;
        [MemoryPackInclude]
        double UserPrice => info.UserPrice;
        [MemoryPackInclude]
        double PayPrice => info.PayPrice;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        string Type => info.Type;


        [MemoryPackConstructor]
        SerializableRelayServerCdkeyOrderInfo(int gb, int speed, string time, string widgetUserId, string orderId, string contact, double costPrice, double price, double userPrice, double payPrice, int count, string type)
        {
            var info = new RelayServerCdkeyOrderInfo
            {
                GB = gb,
                Speed = speed,
                Time = time,
                WidgetUserId = widgetUserId,
                OrderId = orderId,
                Contact = contact,
                CostPrice = costPrice,
                Price = price,
                UserPrice = userPrice,
                PayPrice = payPrice,
                Count = count,
                Type = type
            };
            this.info = info;
        }

        public SerializableRelayServerCdkeyOrderInfo(RelayServerCdkeyOrderInfo info)
        {
            this.info = info;
        }
    }
    public class RelayServerCdkeyOrderInfoFormatter : MemoryPackFormatter<RelayServerCdkeyOrderInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayServerCdkeyOrderInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayServerCdkeyOrderInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayServerCdkeyOrderInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayServerCdkeyOrderInfo>();
            value = wrapped.info;
        }
    }
}
