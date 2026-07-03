using linker.messenger.wlist;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    public class WhiteListInfoFormatter : MemoryPackFormatter<WhiteListInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref WhiteListInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(12);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.UserId);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Remark);
            writer.WriteValue(value.AddTime);
            writer.WriteValue(value.UseTime);
            writer.WriteValue(value.EndTime);
            writer.WriteValue(value.TradeNo);
            writer.WriteValue(value.Nodes);
            writer.WriteValue(value.Bandwidth);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WhiteListInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<int>();
            value.Type = reader.ReadValue<string>();
            value.UserId = reader.ReadValue<string>();
            value.MachineId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Remark = reader.ReadValue<string>();
            value.AddTime = reader.ReadValue<DateTime>();
            value.UseTime = reader.ReadValue<DateTime>();
            value.EndTime = reader.ReadValue<DateTime>();
            value.TradeNo = reader.ReadValue<string>();
            value.Nodes = reader.ReadValue<string[]>();
            value.Bandwidth = reader.ReadValue<double>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(string.Empty);
            writer.WriteValue(value.Data);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WhiteListAddInfo();
            reader.TryReadObjectHeader(out byte count);
            reader.ReadValue<string>();
            value.Data = reader.ReadValue<WhiteListInfo>();
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

            writer.WriteObjectHeader(2);
            writer.WriteValue(string.Empty);
            writer.WriteValue(value.Id);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListDelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WhiteListDelInfo();
            reader.TryReadObjectHeader(out byte count);
            reader.ReadValue<string>();
            value.Id = reader.ReadValue<int>();
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

            writer.WriteObjectHeader(7);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.MachineId);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Remark);
            writer.WriteValue(string.Empty);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListPageRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WhiteListPageRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Type = reader.ReadValue<string>();
            value.MachineId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Remark = reader.ReadValue<string>();
            reader.ReadValue<string>();
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

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
            writer.WriteValue(value.Count);
            writer.WriteValue(value.List);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListPageResultInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WhiteListPageResultInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.List = reader.ReadValue<List<WhiteListInfo>>();
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

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.Enabled);
            writer.WriteValue(value.Type);
            writer.WriteValue(value.Info);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref WhiteListOrderStatusInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new WhiteListOrderStatusInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Enabled = reader.ReadValue<bool>();
            value.Type = reader.ReadValue<string>();
            value.Info = reader.ReadValue<WhiteListInfo>();
        }
    }
}
