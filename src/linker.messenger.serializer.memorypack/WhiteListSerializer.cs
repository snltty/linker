using linker.messenger.wlist;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    [MemoryPackable]
    public readonly partial struct SerializableWhiteListInfo
    {
        [MemoryPackIgnore]
        public readonly WhiteListInfo info;

        [MemoryPackInclude]
        int Id => info.Id;

        [MemoryPackInclude]
        string Type => info.Type;
        [MemoryPackInclude]
        string UserId => info.UserId;
        [MemoryPackInclude]
        string MachineId => info.MachineId;

        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackInclude]
        DateTime AddTime => info.AddTime;
        [MemoryPackInclude]
        DateTime UseTime => info.UseTime;
        [MemoryPackInclude]
        DateTime EndTime => info.EndTime;

        [MemoryPackInclude]
        string TradeNo => info.TradeNo;


        [MemoryPackInclude]
        string[] Nodes => info.Nodes;

        [MemoryPackInclude]
        double Bandwidth => info.Bandwidth;

        [MemoryPackConstructor]
        SerializableWhiteListInfo(int id, string type, string userid, string machineId, string name, string remark, DateTime addTime, DateTime useTime, DateTime endTime, string tradeNo, string[] nodes, double bandwidth)
        {
            var info = new WhiteListInfo
            {
                Id = id,
                Type = type,
                UserId = userid,
                MachineId = machineId,
                AddTime = addTime,
                UseTime = useTime,
                EndTime = endTime,
                Remark = remark,
                TradeNo = tradeNo,
                Nodes = nodes,
                Name = name,
                Bandwidth = bandwidth
            };
            this.info = info;
        }

        public SerializableWhiteListInfo(WhiteListInfo info)
        {
            this.info = info;
        }
    }
    public class WhiteListInfoFormatter : MemoryPackFormatter<WhiteListInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WhiteListInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableWhiteListInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWhiteListInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableWhiteListAddInfo
    {
        [MemoryPackIgnore]
        public readonly WhiteListAddInfo info;

        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        WhiteListInfo Data => info.Data;

        [MemoryPackConstructor]
        SerializableWhiteListAddInfo(string secretKey, WhiteListInfo data)
        {
            var info = new WhiteListAddInfo
            {
                Data = data
            };
            this.info = info;
        }

        public SerializableWhiteListAddInfo(WhiteListAddInfo info)
        {
            this.info = info;
        }
    }
    public class WhiteListAddInfoFormatter : MemoryPackFormatter<WhiteListAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WhiteListAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableWhiteListAddInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWhiteListAddInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableWhiteListDelInfo
    {
        [MemoryPackIgnore]
        public readonly WhiteListDelInfo info;

        [MemoryPackInclude]
        string SecretKey => string.Empty;
        [MemoryPackInclude]
        int Id => info.Id;

        [MemoryPackConstructor]
        SerializableWhiteListDelInfo(string secretKey, int id)
        {
            var info = new WhiteListDelInfo
            {
                Id = id
            };
            this.info = info;
        }

        public SerializableWhiteListDelInfo(WhiteListDelInfo info)
        {
            this.info = info;
        }
    }
    public class WhiteListDelInfoFormatter : MemoryPackFormatter<WhiteListDelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WhiteListDelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableWhiteListDelInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListDelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWhiteListDelInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableWhiteListPageRequestInfo
    {
        [MemoryPackIgnore]
        public readonly WhiteListPageRequestInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;

        [MemoryPackInclude]
        string Type => info.Type;
        [MemoryPackInclude]
        string MachineId => info.MachineId;
        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string Remark => info.Remark;
        [MemoryPackInclude]
        string SecretKey => string.Empty;

        [MemoryPackConstructor]
        SerializableWhiteListPageRequestInfo(int page, int size, string type, string machineid, string name, string remark, string secretKey)
        {
            var info = new WhiteListPageRequestInfo
            {
                Size = size,
                Page = page,
                Type = type,
                MachineId = machineid,
                Remark = remark,
                Name = name,
            };
            this.info = info;
        }

        public SerializableWhiteListPageRequestInfo(WhiteListPageRequestInfo info)
        {
            this.info = info;
        }
    }
    public class WhiteListPageRequestInfoFormatter : MemoryPackFormatter<WhiteListPageRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WhiteListPageRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableWhiteListPageRequestInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListPageRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWhiteListPageRequestInfo>();
            value = wrapped.info;
        }
    }

    [MemoryPackable]
    public readonly partial struct SerializableWhiteListPageResultInfo
    {
        [MemoryPackIgnore]
        public readonly WhiteListPageResultInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<WhiteListInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableWhiteListPageResultInfo(int page, int size, int count, List<WhiteListInfo> list)
        {
            var info = new WhiteListPageResultInfo
            {
                Count = count,
                List = list,
                Size = size,
                Page = page
            };
            this.info = info;
        }

        public SerializableWhiteListPageResultInfo(WhiteListPageResultInfo info)
        {
            this.info = info;
        }
    }
    public class WhiteListPageResultInfoFormatter : MemoryPackFormatter<WhiteListPageResultInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WhiteListPageResultInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableWhiteListPageResultInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListPageResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWhiteListPageResultInfo>();
            value = wrapped.info;
        }
    }



    [MemoryPackable]
    public readonly partial struct SerializableWhiteListOrderStatusInfo
    {
        [MemoryPackIgnore]
        public readonly WhiteListOrderStatusInfo info;

        [MemoryPackInclude]
        bool Enabled => info.Enabled;
        [MemoryPackInclude]
        string Type => info.Type;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        WhiteListInfo Info => info.Info;

        [MemoryPackConstructor]
        SerializableWhiteListOrderStatusInfo(bool enabled, string type, WhiteListInfo info)
        {
            this.info = new WhiteListOrderStatusInfo
            {
                Enabled = enabled,
                Type = type,
                Info = info
            };
        }

        public SerializableWhiteListOrderStatusInfo(WhiteListOrderStatusInfo info)
        {
            this.info = info;
        }
    }
    public class WhiteListOrderStatusInfoFormatter : MemoryPackFormatter<WhiteListOrderStatusInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WhiteListOrderStatusInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableWhiteListOrderStatusInfo(value));
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListOrderStatusInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            var wrapped = reader.ReadPackable<SerializableWhiteListOrderStatusInfo>();
            value = wrapped.info;
        }
    }
}
