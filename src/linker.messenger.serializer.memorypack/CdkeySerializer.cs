using linker.messenger.cdkey;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{

    [MemoryPackable]
    public readonly partial struct SerializableCdkeyInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyInfo info;

        [MemoryPackInclude]
        int Id => info.Id;

        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        long LastBytes => info.LastBytes;

        [MemoryPackConstructor]
        SerializableCdkeyInfo(int id, double bandwidth, long lastBytes)
        {
            var info = new CdkeyInfo
            {
                Id = id,
                Bandwidth = bandwidth,
                LastBytes = lastBytes
            };
            this.info = info;
        }

        public SerializableCdkeyInfo(CdkeyInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyInfoFormatter : MemoryPackFormatter<CdkeyInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableCdkeyStoreInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyStoreInfo info;

        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;
        [MemoryPackInclude]
        long LastBytes => info.LastBytes;

        [MemoryPackInclude]
        int Id => info.Id;
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

        [MemoryPackInclude]
        string Type => info.Type;

        [MemoryPackInclude]
        string[] Values => info.Values;

        [MemoryPackConstructor]
        SerializableCdkeyStoreInfo(double bandwidth, long lastBytes, int id, string userid, DateTime addTime, DateTime startTime, DateTime endTime, DateTime useTime, long maxBytes, double costPrice, double price, double userPrice, double payPrice, string remark, string orderId, string contact, bool deleted, string type, string[] values)
        {
            var info = new CdkeyStoreInfo
            {
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
                Deleted = deleted,
                Type = type,
                Values = values
            };
            this.info = info;
        }

        public SerializableCdkeyStoreInfo(CdkeyStoreInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyStoreInfoFormatter : MemoryPackFormatter<CdkeyStoreInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyStoreInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyStoreInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyStoreInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyStoreInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableCdkeyAddInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyAddInfo info;

        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        CdkeyStoreInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableCdkeyAddInfo(string secretKey, CdkeyStoreInfo data)
        {
            var info = new CdkeyAddInfo
            {
                Data = data
            };
            this.info = info;
        }

        public SerializableCdkeyAddInfo(CdkeyAddInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyAddInfoFormatter : MemoryPackFormatter<CdkeyAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyAddInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyAddInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableCdkeyDelInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyDelInfo info;

        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude]
        string UserId => info.UserId;
        [MemoryPackInclude]
        int Id => info.Id;

        [MemoryPackConstructor]
        SerializableCdkeyDelInfo(string secretKey, string userid, int id)
        {
            var info = new CdkeyDelInfo
            {
                UserId = userid,
                Id = id
            };
            this.info = info;
        }

        public SerializableCdkeyDelInfo(CdkeyDelInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyDelInfoFormatter : MemoryPackFormatter<CdkeyDelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyDelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyDelInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyDelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyDelInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableCdkeyPageRequestInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyPageRequestInfo info;

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
        string SecretKey => string.Empty;
        [MemoryPackInclude]
        CdkeyPageRequestFlag Flag => info.Flag;

        [MemoryPackInclude]
        string Type => info.Type;

        [MemoryPackConstructor]
        SerializableCdkeyPageRequestInfo(int page, int size, string order, string sort, string userid, string remark, string orderid, string contact, string secretKey, CdkeyPageRequestFlag flag, string type)
        {
            var info = new CdkeyPageRequestInfo
            {
                Sort = sort,
                Order = order,
                Size = size,
                Page = page,
                UserId = userid,
                Remark = remark,
                OrderId = orderid,
                Contact = contact,
                Flag = flag,
                Type = type
            };
            this.info = info;
        }

        public SerializableCdkeyPageRequestInfo(CdkeyPageRequestInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyPageRequestInfoFormatter : MemoryPackFormatter<CdkeyPageRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyPageRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyPageRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyPageRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyPageRequestInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableCdkeyPageResultInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyPageResultInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<CdkeyStoreInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableCdkeyPageResultInfo(int page, int size, int count, List<CdkeyStoreInfo> list)
        {
            var info = new CdkeyPageResultInfo
            {
                Count = count,
                List = list,
                Size = size,
                Page = page
            };
            this.info = info;
        }

        public SerializableCdkeyPageResultInfo(CdkeyPageResultInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyPageResultInfoFormatter : MemoryPackFormatter<CdkeyPageResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyPageResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyPageResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyPageResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyPageResultInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableCdkeyImportInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyImportInfo info;

        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude]
        string UserId => info.UserId;
        [MemoryPackInclude]
        string Base64 => info.Base64;

        [MemoryPackConstructor]
        SerializableCdkeyImportInfo(string secretKey, string userid, string base64)
        {
            var info = new CdkeyImportInfo
            {
                UserId = userid,
                Base64 = base64
            };
            this.info = info;
        }

        public SerializableCdkeyImportInfo(CdkeyImportInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyImportInfoFormatter : MemoryPackFormatter<CdkeyImportInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyImportInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyImportInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyImportInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyImportInfo>();
            value = wrapped.info;
        }
    }


    [MemoryPackable]
    public readonly partial struct SerializableCdkeyTestResultInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyTestResultInfo info;

        [MemoryPackInclude, MemoryPackAllowSerialize]
        CdkeyOrderInfo Order => info.Order;
        [MemoryPackInclude]
        string Cdkey => info.Cdkey;
        [MemoryPackInclude]
        List<string> Field => info.Field;

        [MemoryPackConstructor]
        SerializableCdkeyTestResultInfo(CdkeyOrderInfo order, string cdkey, List<string> field)
        {
            var info = new CdkeyTestResultInfo
            {
                Order = order,
                Cdkey = cdkey,
                Field = field
            };
            this.info = info;
        }

        public SerializableCdkeyTestResultInfo(CdkeyTestResultInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyTestResultInfoFormatter : MemoryPackFormatter<CdkeyTestResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyTestResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyTestResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyTestResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyTestResultInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableCdkeyOrderInfo
    {
        [MemoryPackIgnore]
        public readonly CdkeyOrderInfo info;

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
        [MemoryPackInclude]
        string[] Values => info.Values;

        [MemoryPackConstructor]
        SerializableCdkeyOrderInfo(int gb, int speed, string time, string widgetUserId, string orderId, string contact, double costPrice, double price, double userPrice, double payPrice, int count, string type, string[] values)
        {
            var info = new CdkeyOrderInfo
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
                Type = type,
                 Values= values
            };
            this.info = info;
        }

        public SerializableCdkeyOrderInfo(CdkeyOrderInfo info)
        {
            this.info = info;
        }
    }
    public class CdkeyOrderInfoFormatter : MemoryPackFormatter<CdkeyOrderInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref CdkeyOrderInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableCdkeyOrderInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref CdkeyOrderInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableCdkeyOrderInfo>();
            value = wrapped.info;
        }
    }
}
