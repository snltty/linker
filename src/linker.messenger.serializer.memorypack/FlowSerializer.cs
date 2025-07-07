using MemoryPack;
using linker.messenger.flow;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableFlowItemInfo
    {
        [MemoryPackIgnore]
        public readonly FlowItemInfo info;

        [MemoryPackInclude]
        long ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        long SendtBytes => info.SendtBytes;

        [MemoryPackConstructor]
        SerializableFlowItemInfo(long receiveBytes, long sendtBytes)
        {
            var info = new FlowItemInfo { ReceiveBytes = receiveBytes, SendtBytes = sendtBytes };
            this.info = info;
        }

        public SerializableFlowItemInfo(FlowItemInfo info)
        {
            this.info = info;
        }
    }
    public class FlowItemInfoFormatter : MemoryPackFormatter<FlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFlowItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFlowItemInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableFlowReportNetInfo
    {
        [MemoryPackIgnore]
        public readonly FlowReportNetInfo info;

        [MemoryPackInclude]
        string City => info.City;
        [MemoryPackInclude]
        double Lat => info.Lat;
        [MemoryPackInclude]
        double Lon => info.Lon;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackConstructor]
        SerializableFlowReportNetInfo(string city, double lat, double lon, int count)
        {
            var info = new FlowReportNetInfo
            {
                City = city,
                Lat = lat,
                Lon = lon,
                Count = count
            };
            this.info = info;
        }

        public SerializableFlowReportNetInfo(FlowReportNetInfo tunnelCompactInfo)
        {
            this.info = tunnelCompactInfo;
        }
    }
    public class FlowReportNetInfoFormatter : MemoryPackFormatter<FlowReportNetInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FlowReportNetInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFlowReportNetInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FlowReportNetInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFlowReportNetInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableFlowInfo
    {
        [MemoryPackIgnore]
        public readonly FlowInfo info;

        [MemoryPackInclude]
        Dictionary<string, FlowItemInfo> Items => info.Items;

        [MemoryPackInclude]
        DateTime Start => info.Start;

        [MemoryPackInclude]
        DateTime Now => info.Now;

        [MemoryPackConstructor]
        SerializableFlowInfo(Dictionary<string, FlowItemInfo> items, DateTime start, DateTime now)
        {
            var info = new FlowInfo { Items = items, Now = now, Start = start };
            this.info = info;
        }

        public SerializableFlowInfo(FlowInfo info)
        {
            this.info = info;
        }
    }
    public class FlowInfoFormatter : MemoryPackFormatter<FlowInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FlowInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableFlowInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref FlowInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableFlowInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayFlowItemInfo
    {
        [MemoryPackIgnore]
        public readonly RelayFlowItemInfo info;

        [MemoryPackInclude]
        long ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        long SendtBytes => info.SendtBytes;

        [MemoryPackInclude]
        long DiffReceiveBytes => info.DiffReceiveBytes;

        [MemoryPackInclude]
        long DiffSendtBytes => info.DiffSendtBytes;

        [MemoryPackInclude]
        string FromName => info.FromName;

        [MemoryPackInclude]
        string ToName => info.ToName;

        [MemoryPackConstructor]
        SerializableRelayFlowItemInfo(long receiveBytes, long sendtBytes, long diffReceiveBytes, long diffSendtBytes, string fromName, string toName)
        {
            var info = new RelayFlowItemInfo
            {
                ReceiveBytes = receiveBytes,
                SendtBytes = sendtBytes,
                DiffReceiveBytes = diffReceiveBytes,
                DiffSendtBytes = diffSendtBytes,
                FromName = fromName,
                ToName = toName
            };
            this.info = info;
        }

        public SerializableRelayFlowItemInfo(RelayFlowItemInfo info)
        {
            this.info = info;
        }
    }
    public class RelayFlowItemInfoFormatter : MemoryPackFormatter<RelayFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayFlowItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayFlowItemInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayFlowRequestInfo
    {
        [MemoryPackIgnore]
        public readonly RelayFlowRequestInfo info;

        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackInclude]
        string SecretKey => info.SecretKey;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        RelayFlowOrder Order => info.Order;

        [MemoryPackInclude]
        RelayFlowOrderType OrderType => info.OrderType;

        [MemoryPackConstructor]
        SerializableRelayFlowRequestInfo(string key, string secretKey, int page, int pageSize, RelayFlowOrder order, RelayFlowOrderType orderType)
        {
            var info = new RelayFlowRequestInfo
            {
                Key = key,
                Order = order,
                OrderType = orderType,
                Page = page,
                PageSize = pageSize,
                SecretKey = secretKey
            };
            this.info = info;
        }

        public SerializableRelayFlowRequestInfo(RelayFlowRequestInfo info)
        {
            this.info = info;
        }
    }
    public class RelayFlowRequestInfoFormatter : MemoryPackFormatter<RelayFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayFlowRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayFlowRequestInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableRelayFlowResponseInfo
    {
        [MemoryPackIgnore]
        public readonly RelayFlowResponseInfo info;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackInclude]
        List<RelayFlowItemInfo> Data => info.Data;

        [MemoryPackConstructor]
        SerializableRelayFlowResponseInfo(int page, int pageSize, int count, List<RelayFlowItemInfo> data)
        {
            var info = new RelayFlowResponseInfo
            {
                Page = page,
                PageSize = pageSize,
                Count = count,
                Data = data
            };
            this.info = info;
        }

        public SerializableRelayFlowResponseInfo(RelayFlowResponseInfo info)
        {
            this.info = info;
        }
    }
    public class RelayFlowResponseInfoFormatter : MemoryPackFormatter<RelayFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref RelayFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableRelayFlowResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref RelayFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableRelayFlowResponseInfo>();
            value = wrapped.info;
        }
    }




    [MemoryPackable]
    public readonly partial struct SerializableSForwardFlowItemInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardFlowItemInfo info;

        [MemoryPackInclude]
        long ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        long SendtBytes => info.SendtBytes;

        [MemoryPackInclude]
        long DiffReceiveBytes => info.DiffReceiveBytes;

        [MemoryPackInclude]
        long DiffSendtBytes => info.DiffSendtBytes;

        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackConstructor]
        SerializableSForwardFlowItemInfo(long receiveBytes, long sendtBytes, long diffReceiveBytes, long diffSendtBytes, string key)
        {
            var info = new SForwardFlowItemInfo
            {
                ReceiveBytes = receiveBytes,
                SendtBytes = sendtBytes,

                DiffReceiveBytes = diffReceiveBytes,
                DiffSendtBytes = diffSendtBytes,
                Key = key
            };
            this.info = info;
        }

        public SerializableSForwardFlowItemInfo(SForwardFlowItemInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardFlowItemInfoFormatter : MemoryPackFormatter<SForwardFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardFlowItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardFlowItemInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSForwardFlowRequestInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardFlowRequestInfo info;

        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        SForwardFlowOrder Order => info.Order;

        [MemoryPackInclude]
        SForwardFlowOrderType OrderType => info.OrderType;

        [MemoryPackConstructor]
        SerializableSForwardFlowRequestInfo(string key, string machineId, int page, int pageSize, SForwardFlowOrder order, SForwardFlowOrderType orderType)
        {
            var info = new SForwardFlowRequestInfo
            {
                Key = key,
                Order = order,
                OrderType = orderType,
                Page = page,
                PageSize = pageSize,
                MachineId = machineId
            };
            this.info = info;
        }

        public SerializableSForwardFlowRequestInfo(SForwardFlowRequestInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardFlowRequestInfoFormatter : MemoryPackFormatter<SForwardFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardFlowRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardFlowRequestInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSForwardFlowResponseInfo
    {
        [MemoryPackIgnore]
        public readonly SForwardFlowResponseInfo info;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackInclude]
        List<SForwardFlowItemInfo> Data => info.Data;

        [MemoryPackConstructor]
        SerializableSForwardFlowResponseInfo(int page, int pageSize, int count, List<SForwardFlowItemInfo> data)
        {
            var info = new SForwardFlowResponseInfo
            {
                Page = page,
                PageSize = pageSize,
                Count = count,
                Data = data
            };
            this.info = info;
        }

        public SerializableSForwardFlowResponseInfo(SForwardFlowResponseInfo info)
        {
            this.info = info;
        }
    }
    public class SForwardFlowResponseInfoFormatter : MemoryPackFormatter<SForwardFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref SForwardFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSForwardFlowResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref SForwardFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableSForwardFlowResponseInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableForwardFlowItemInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardFlowItemInfo info;

        [MemoryPackInclude]
        long ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        long SendtBytes => info.SendtBytes;

        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Target => info.Target;

        [MemoryPackConstructor]
        SerializableForwardFlowItemInfo(long receiveBytes, long sendtBytes, string key, IPEndPoint target)
        {
            var info = new ForwardFlowItemInfo
            {
                ReceiveBytes = receiveBytes,
                SendtBytes = sendtBytes,
                Key = key,
                Target = target
            };
            this.info = info;
        }

        public SerializableForwardFlowItemInfo(ForwardFlowItemInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardFlowItemInfoFormatter : MemoryPackFormatter<ForwardFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardFlowItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableForwardFlowItemInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableForwardFlowRequestInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardFlowRequestInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        ForwardFlowOrder Order => info.Order;

        [MemoryPackInclude]
        ForwardFlowOrderType OrderType => info.OrderType;

        [MemoryPackConstructor]
        SerializableForwardFlowRequestInfo(string machineId, int page, int pageSize, ForwardFlowOrder order, ForwardFlowOrderType orderType)
        {
            var info = new ForwardFlowRequestInfo
            {
                MachineId = machineId,
                Order = order,
                OrderType = orderType,
                Page = page,
                PageSize = pageSize
            };
            this.info = info;
        }

        public SerializableForwardFlowRequestInfo(ForwardFlowRequestInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardFlowRequestInfoFormatter : MemoryPackFormatter<ForwardFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardFlowRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableForwardFlowRequestInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableForwardFlowResponseInfo
    {
        [MemoryPackIgnore]
        public readonly ForwardFlowResponseInfo info;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackInclude]
        List<ForwardFlowItemInfo> Data => info.Data;

        [MemoryPackConstructor]
        SerializableForwardFlowResponseInfo(int page, int pageSize, int count, List<ForwardFlowItemInfo> data)
        {
            var info = new ForwardFlowResponseInfo
            {
                Page = page,
                PageSize = pageSize,
                Count = count,
                Data = data
            };
            this.info = info;
        }

        public SerializableForwardFlowResponseInfo(ForwardFlowResponseInfo info)
        {
            this.info = info;
        }
    }
    public class ForwardFlowResponseInfoFormatter : MemoryPackFormatter<ForwardFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ForwardFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableForwardFlowResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ForwardFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableForwardFlowResponseInfo>();
            value = wrapped.info;
        }
    }
}
