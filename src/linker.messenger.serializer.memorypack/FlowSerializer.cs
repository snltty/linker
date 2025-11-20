using MemoryPack;
using linker.messenger.flow;
using System.Net;
using linker.tunnel.connection;

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

            value = new FlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
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

            value = new FlowReportNetInfo();
            reader.TryReadObjectHeader(out byte count);
            value.City = reader.ReadValue<string>();
            value.Lat = reader.ReadValue<double>();
            value.Lon = reader.ReadValue<double>();
            value.Count = reader.ReadValue<int>();
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

            value = new FlowInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Items = reader.ReadValue<Dictionary<string, FlowItemInfo>>();
            value.Start = reader.ReadValue<DateTime>();
            value.Now = reader.ReadValue<DateTime>();
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

            value = new RelayFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.FromName = reader.ReadValue<string>();
            value.ToName = reader.ReadValue<string>();
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

            value = new RelayFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Key = reader.ReadValue<string>();
            value.SecretKey = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<RelayFlowOrder>();
            value.OrderType = reader.ReadValue<RelayFlowOrderType>();
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

            value = new RelayFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<RelayFlowItemInfo>>();
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

            value = new SForwardFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
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

            value = new SForwardFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Key = reader.ReadValue<string>();
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<SForwardFlowOrder>();
            value.OrderType = reader.ReadValue<SForwardFlowOrderType>();
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

            value = new SForwardFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<SForwardFlowItemInfo>>();
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
        long DiffReceiveBytes => info.DiffReceiveBytes;

        [MemoryPackInclude]
        long DiffSendtBytes => info.DiffSendtBytes;


        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Target => info.Target;

        [MemoryPackConstructor]
        SerializableForwardFlowItemInfo(long receiveBytes, long sendtBytes, long diffReceiveBytes, long diffSendtBytes, string key, IPEndPoint target)
        {
            var info = new ForwardFlowItemInfo
            {
                ReceiveBytes = receiveBytes,
                SendtBytes = sendtBytes,
                DiffReceiveBytes = diffReceiveBytes,
                DiffSendtBytes = diffSendtBytes,
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

            value = new ForwardFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
            value.Target = reader.ReadValue<IPEndPoint>();

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

            value = new ForwardFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<ForwardFlowOrder>();
            value.OrderType = reader.ReadValue<ForwardFlowOrderType>();
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

            value = new ForwardFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<ForwardFlowItemInfo>>();
        }
    }




    [MemoryPackable]
    public readonly partial struct SerializableSocks5FlowItemInfo
    {
        [MemoryPackIgnore]
        public readonly Socks5FlowItemInfo info;

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

        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Target => info.Target;

        [MemoryPackConstructor]
        SerializableSocks5FlowItemInfo(long receiveBytes, long sendtBytes, long diffReceiveBytes, long diffSendtBytes, string key, IPEndPoint target)
        {
            var info = new Socks5FlowItemInfo
            {
                ReceiveBytes = receiveBytes,
                SendtBytes = sendtBytes,
                DiffReceiveBytes = diffReceiveBytes,
                DiffSendtBytes = diffSendtBytes,
                Key = key,
                Target = target
            };
            this.info = info;
        }

        public SerializableSocks5FlowItemInfo(Socks5FlowItemInfo info)
        {
            this.info = info;
        }
    }
    public class Socks5FlowItemInfoFormatter : MemoryPackFormatter<Socks5FlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5FlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSocks5FlowItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5FlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new Socks5FlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.DiffReceiveBytes = reader.ReadValue<long>();
            value.DiffSendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
            value.Target = reader.ReadValue<IPEndPoint>();

        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSocks5FlowRequestInfo
    {
        [MemoryPackIgnore]
        public readonly Socks5FlowRequestInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        Socks5FlowOrder Order => info.Order;

        [MemoryPackInclude]
        Socks5FlowOrderType OrderType => info.OrderType;

        [MemoryPackConstructor]
        SerializableSocks5FlowRequestInfo(string machineId, int page, int pageSize, Socks5FlowOrder order, Socks5FlowOrderType orderType)
        {
            var info = new Socks5FlowRequestInfo
            {
                MachineId = machineId,
                Order = order,
                OrderType = orderType,
                Page = page,
                PageSize = pageSize
            };
            this.info = info;
        }

        public SerializableSocks5FlowRequestInfo(Socks5FlowRequestInfo info)
        {
            this.info = info;
        }
    }
    public class Socks5FlowRequestInfoFormatter : MemoryPackFormatter<Socks5FlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5FlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSocks5FlowRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5FlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new Socks5FlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<Socks5FlowOrder>();
            value.OrderType = reader.ReadValue<Socks5FlowOrderType>();
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableSocks5FlowResponseInfo
    {
        [MemoryPackIgnore]
        public readonly Socks5FlowResponseInfo info;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackInclude]
        List<Socks5FlowItemInfo> Data => info.Data;

        [MemoryPackConstructor]
        SerializableSocks5FlowResponseInfo(int page, int pageSize, int count, List<Socks5FlowItemInfo> data)
        {
            var info = new Socks5FlowResponseInfo
            {
                Page = page,
                PageSize = pageSize,
                Count = count,
                Data = data
            };
            this.info = info;
        }

        public SerializableSocks5FlowResponseInfo(Socks5FlowResponseInfo info)
        {
            this.info = info;
        }
    }
    public class Socks5FlowResponseInfoFormatter : MemoryPackFormatter<Socks5FlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Socks5FlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableSocks5FlowResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref Socks5FlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new Socks5FlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<Socks5FlowItemInfo>>();
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableTunnelFlowItemInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelFlowItemInfo info;

        [MemoryPackInclude]
        long ReceiveBytes => info.ReceiveBytes;

        [MemoryPackInclude]
        long SendtBytes => info.SendtBytes;


        [MemoryPackInclude]
        string Key => info.Key;

        [MemoryPackInclude]
        string TransitionId => info.TransitionId;

        [MemoryPackInclude]
        TunnelDirection Direction => info.Direction;
        [MemoryPackInclude]
        TunnelType Type => info.Type;
        [MemoryPackInclude]
        TunnelMode Mode => info.Mode;

        [MemoryPackConstructor]
        SerializableTunnelFlowItemInfo(long receiveBytes, long sendtBytes, string key, string transitionId, TunnelDirection direction, TunnelType type, TunnelMode mode)
        {
            var info = new TunnelFlowItemInfo
            {
                ReceiveBytes = receiveBytes,
                SendtBytes = sendtBytes,
                Key = key,
                TransitionId = transitionId,
                Direction = direction,
                Type = type,
                Mode = mode
            };
            this.info = info;
        }

        public SerializableTunnelFlowItemInfo(TunnelFlowItemInfo info)
        {
            this.info = info;
        }
    }
    public class TunnelFlowItemInfoFormatter : MemoryPackFormatter<TunnelFlowItemInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelFlowItemInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelFlowItemInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelFlowItemInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelFlowItemInfo();
            reader.TryReadObjectHeader(out byte count);
            value.ReceiveBytes = reader.ReadValue<long>();
            value.SendtBytes = reader.ReadValue<long>();
            value.Key = reader.ReadValue<string>();
            value.TransitionId = reader.ReadValue<string>();
            value.Direction = reader.ReadValue<TunnelDirection>();
            value.Type = reader.ReadValue<TunnelType>();
            value.Mode = reader.ReadValue<TunnelMode>();
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableTunnelFlowRequestInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelFlowRequestInfo info;

        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        TunnelFlowOrder Order => info.Order;

        [MemoryPackInclude]
        TunnelFlowOrderType OrderType => info.OrderType;

        [MemoryPackConstructor]
        SerializableTunnelFlowRequestInfo(string machineId, int page, int pageSize, TunnelFlowOrder order, TunnelFlowOrderType orderType)
        {
            var info = new TunnelFlowRequestInfo
            {
                MachineId = machineId,
                Order = order,
                OrderType = orderType,
                Page = page,
                PageSize = pageSize
            };
            this.info = info;
        }

        public SerializableTunnelFlowRequestInfo(TunnelFlowRequestInfo info)
        {
            this.info = info;
        }
    }
    public class TunnelFlowRequestInfoFormatter : MemoryPackFormatter<TunnelFlowRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelFlowRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelFlowRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelFlowRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelFlowRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.MachineId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Order = reader.ReadValue<TunnelFlowOrder>();
            value.OrderType = reader.ReadValue<TunnelFlowOrderType>();
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableTunnelFlowResponseInfo
    {
        [MemoryPackIgnore]
        public readonly TunnelFlowResponseInfo info;

        [MemoryPackInclude]
        int Page => info.Page;

        [MemoryPackInclude]
        int PageSize => info.PageSize;

        [MemoryPackInclude]
        int Count => info.Count;

        [MemoryPackInclude]
        List<TunnelFlowItemInfo> Data => info.Data;

        [MemoryPackConstructor]
        SerializableTunnelFlowResponseInfo(int page, int pageSize, int count, List<TunnelFlowItemInfo> data)
        {
            var info = new TunnelFlowResponseInfo
            {
                Page = page,
                PageSize = pageSize,
                Count = count,
                Data = data
            };
            this.info = info;
        }

        public SerializableTunnelFlowResponseInfo(TunnelFlowResponseInfo info)
        {
            this.info = info;
        }
    }
    public class TunnelFlowResponseInfoFormatter : MemoryPackFormatter<TunnelFlowResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref TunnelFlowResponseInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableTunnelFlowResponseInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref TunnelFlowResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new TunnelFlowResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.PageSize = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.Data = reader.ReadValue<List<TunnelFlowItemInfo>>();
        }
    }
}
