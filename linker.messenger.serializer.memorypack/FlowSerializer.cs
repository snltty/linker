using MemoryPack;
using linker.messenger.flow;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableFlowItemInfo
    {
        [MemoryPackIgnore]
        public readonly FlowItemInfo info;

        [MemoryPackInclude]
        ulong ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        ulong SendtBytes => info.SendtBytes;

        [MemoryPackConstructor]
        SerializableFlowItemInfo(ulong receiveBytes, ulong sendtBytes)
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
        ulong ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        ulong SendtBytes => info.SendtBytes;

        [MemoryPackInclude]
        ulong DiffReceiveBytes => info.DiffReceiveBytes;

        [MemoryPackInclude]
        ulong DiffSendtBytes => info.DiffSendtBytes;

        [MemoryPackInclude]
        string FromName => info.FromName;

        [MemoryPackInclude]
        string ToName => info.ToName;

        [MemoryPackConstructor]
        SerializableRelayFlowItemInfo(ulong receiveBytes, ulong sendtBytes, ulong diffReceiveBytes, ulong diffSendtBytes, string fromName, string toName)
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
        ulong ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        ulong SendtBytes => info.SendtBytes;

        [MemoryPackInclude]
        ulong DiffReceiveBytes => info.DiffReceiveBytes;

        [MemoryPackInclude]
        ulong DiffSendtBytes => info.DiffSendtBytes;

        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackConstructor]
        SerializableSForwardFlowItemInfo(ulong receiveBytes, ulong sendtBytes, ulong diffReceiveBytes, ulong diffSendtBytes, string key)
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
        string SecretKey => info.SecretKey;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        SForwardFlowOrder Order => info.Order;

        [MemoryPackInclude]
        SForwardFlowOrderType OrderType => info.OrderType;

        [MemoryPackConstructor]
        SerializableSForwardFlowRequestInfo(string key, string secretKey, int page, int pageSize, SForwardFlowOrder order, SForwardFlowOrderType orderType)
        {
            var info = new SForwardFlowRequestInfo
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
}
