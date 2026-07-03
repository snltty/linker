using linker.messenger.node;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{
    public class NodeShareInfoFormatter : MemoryPackFormatter<NodeShareInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref NodeShareInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Name);
            writer.WriteValue(value.Host);
            writer.WriteValue(value.MasterKey);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref NodeShareInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new NodeShareInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Name = reader.ReadValue<string>();
            value.Host = reader.ReadValue<string>();
            value.MasterKey = reader.ReadValue<string>();
        }
    }

    public class MastersRequestInfoFormatter : MemoryPackFormatter<MastersRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MastersRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(3);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MastersRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MastersRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
        }
    }

    public class MasterConnInfoFormatter : MemoryPackFormatter<MasterConnInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MasterConnInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Addr);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MasterConnInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MasterConnInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Addr = reader.ReadValue<IPEndPoint>();
        }
    }

    public class MastersResponseInfoFormatter : MemoryPackFormatter<MastersResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MastersResponseInfo value)
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

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MastersResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MastersResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.List = reader.ReadValue<List<MasterConnInfo>>();
        }
    }


    public class MasterDenyStoreRequestInfoFormatter : MemoryPackFormatter<MasterDenyStoreRequestInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MasterDenyStoreRequestInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Page);
            writer.WriteValue(value.Size);
            writer.WriteValue(value.Str);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MasterDenyStoreRequestInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MasterDenyStoreRequestInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Str = reader.ReadValue<string>();
        }
    }
   
    public class MasterDenyStoreResponseInfoFormatter : MemoryPackFormatter<MasterDenyStoreResponseInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MasterDenyStoreResponseInfo value)
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

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MasterDenyStoreResponseInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MasterDenyStoreResponseInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Page = reader.ReadValue<int>();
            value.Size = reader.ReadValue<int>();
            value.Count = reader.ReadValue<int>();
            value.List = reader.ReadValue<List<MasterDenyStoreInfo>>();
        }
    }

    public class MasterDenyStoreInfoFormatter : MemoryPackFormatter<MasterDenyStoreInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MasterDenyStoreInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(5);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Ip);
            writer.WriteValue(value.Plus);
            writer.WriteValue(value.Str);
            writer.WriteValue(value.Remark);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MasterDenyStoreInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MasterDenyStoreInfo();
            reader.TryReadObjectHeader(out byte count);
            value.Id = reader.ReadValue<int>();
            value.Ip = reader.ReadValue<uint>();
            value.Plus = reader.ReadValue<uint>();
            value.Str = reader.ReadValue<string>();
            value.Remark = reader.ReadValue<string>();
        }
    }

    public class MasterDenyAddInfoFormatter : MemoryPackFormatter<MasterDenyAddInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MasterDenyAddInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(4);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Id);
            writer.WriteValue(value.Str);
            writer.WriteValue(value.Remark);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MasterDenyAddInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MasterDenyAddInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Id = reader.ReadValue<int>();
            value.Str = reader.ReadValue<string>();
            value.Remark = reader.ReadValue<string>();
        }
    }

    public class MasterDenyDelInfoFormatter : MemoryPackFormatter<MasterDenyDelInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref MasterDenyDelInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteValue(value.NodeId);
            writer.WriteValue(value.Id);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref MasterDenyDelInfo value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1); // skip null block
                value = null;
                return;
            }

            value = new MasterDenyDelInfo();
            reader.TryReadObjectHeader(out byte count);
            value.NodeId = reader.ReadValue<string>();
            value.Id = reader.ReadValue<int>();
        }
    }

}
