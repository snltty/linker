using linker.messenger.node;
using MemoryPack;
using System.Net;

namespace linker.messenger.serializer.memorypack
{

    [MemoryPackable]
    public readonly partial struct SerializableNodeShareInfo
    {
        [MemoryPackIgnore]
        public readonly NodeShareInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;

        [MemoryPackInclude]
        string Name => info.Name;
        [MemoryPackInclude]
        string Host => info.Host;
        [MemoryPackInclude]
        string MasterKey => info.MasterKey;

        [MemoryPackConstructor]
        SerializableNodeShareInfo(string nodeId, string name, string host, string masterKey)
        {
            var info = new NodeShareInfo
            {
                NodeId = nodeId,
                Host = host,
                Name = name,
                MasterKey = masterKey
            };
            this.info = info;
        }

        public SerializableNodeShareInfo(NodeShareInfo info)
        {
            this.info = info;
        }
    }
    public class NodeShareInfoFormatter : MemoryPackFormatter<NodeShareInfo>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref NodeShareInfo value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WritePackable(new SerializableNodeShareInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializableMastersRequestInfo
    {
        [MemoryPackIgnore]
        public readonly MastersRequestInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;

        [MemoryPackConstructor]
        SerializableMastersRequestInfo(string nodeId, int page, int size)
        {
            var info = new MastersRequestInfo
            {
                NodeId = nodeId,
                Page = page,
                Size = size
            };
            this.info = info;
        }

        public SerializableMastersRequestInfo(MastersRequestInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMastersRequestInfo(value));
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

    [MemoryPackable]
    public readonly partial struct SerializableMasterConnInfo
    {
        [MemoryPackIgnore]
        public readonly MasterConnInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;
        [MemoryPackInclude, MemoryPackAllowSerialize]
        IPEndPoint Addr => info.Addr;

        [MemoryPackConstructor]
        SerializableMasterConnInfo(string nodeId, IPEndPoint addr)
        {
            var info = new MasterConnInfo
            {
                NodeId = nodeId,
                Addr = addr
            };
            this.info = info;
        }

        public SerializableMasterConnInfo(MasterConnInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMasterConnInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializableMastersResponseInfo
    {
        [MemoryPackIgnore]
        public readonly MastersResponseInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<MasterConnInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableMastersResponseInfo(int page, int size, int count, List<MasterConnInfo> list)
        {
            var info = new MastersResponseInfo
            {
                Page = page,
                Size = size,
                Count = count,
                List = list
            };
            this.info = info;
        }

        public SerializableMastersResponseInfo(MastersResponseInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMastersResponseInfo(value));
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





    [MemoryPackable]
    public readonly partial struct SerializableMasterDenyStoreRequestInfo
    {
        [MemoryPackIgnore]
        public readonly MasterDenyStoreRequestInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        string Str => info.Str;

        [MemoryPackConstructor]
        SerializableMasterDenyStoreRequestInfo(string nodeId, int page, int size, string str)
        {
            var info = new MasterDenyStoreRequestInfo
            {
                NodeId = nodeId,
                Page = page,
                Size = size,
                Str = str
            };
            this.info = info;
        }

        public SerializableMasterDenyStoreRequestInfo(MasterDenyStoreRequestInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMasterDenyStoreRequestInfo(value));
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
    [MemoryPackable]
    public readonly partial struct SerializableMasterDenyStoreResponseInfo
    {
        [MemoryPackIgnore]
        public readonly MasterDenyStoreResponseInfo info;

        [MemoryPackInclude]
        int Page => info.Page;
        [MemoryPackInclude]
        int Size => info.Size;
        [MemoryPackInclude]
        int Count => info.Count;
        [MemoryPackInclude]
        List<MasterDenyStoreInfo> List => info.List;

        [MemoryPackConstructor]
        SerializableMasterDenyStoreResponseInfo(int page, int size, int count, List<MasterDenyStoreInfo> list)
        {
            var info = new MasterDenyStoreResponseInfo
            {
                Page = page,
                Size = size,
                Count = count,
                List = list
            };
            this.info = info;
        }

        public SerializableMasterDenyStoreResponseInfo(MasterDenyStoreResponseInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMasterDenyStoreResponseInfo(value));
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

    [MemoryPackable]
    public readonly partial struct SerializableMasterDenyStoreInfo
    {
        [MemoryPackIgnore]
        public readonly MasterDenyStoreInfo info;

        [MemoryPackInclude]
        int Id => info.Id;
        [MemoryPackInclude]
        uint Ip => info.Ip;
        [MemoryPackInclude]
        uint Plus => info.Plus;
        [MemoryPackInclude]
        string Str => info.Str;
        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackConstructor]
        SerializableMasterDenyStoreInfo(int id, uint ip, uint plus, string str, string remark)
        {
            var info = new MasterDenyStoreInfo
            {
                Id = id,
                Ip = ip,
                Plus = plus,
                Str = str,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableMasterDenyStoreInfo(MasterDenyStoreInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMasterDenyStoreInfo(value));
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


    [MemoryPackable]
    public readonly partial struct SerializableMasterDenyAddInfo
    {
        [MemoryPackIgnore]
        public readonly MasterDenyAddInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;

        [MemoryPackInclude]
        int Id => info.Id;

        [MemoryPackInclude]
        string Str => info.Str;
        [MemoryPackInclude]
        string Remark => info.Remark;

        [MemoryPackConstructor]
        SerializableMasterDenyAddInfo(string nodeid, int id, string str, string remark)
        {
            var info = new MasterDenyAddInfo
            {
                NodeId = nodeid,
                Id = id,
                Str = str,
                Remark = remark
            };
            this.info = info;
        }

        public SerializableMasterDenyAddInfo(MasterDenyAddInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMasterDenyAddInfo(value));
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

    [MemoryPackable]
    public readonly partial struct SerializableMasterDenyDelInfo
    {
        [MemoryPackIgnore]
        public readonly MasterDenyDelInfo info;

        [MemoryPackInclude]
        string NodeId => info.NodeId;

        [MemoryPackInclude]
        int Id => info.Id;

        [MemoryPackConstructor]
        SerializableMasterDenyDelInfo(string nodeid, int id)
        {
            var info = new MasterDenyDelInfo
            {
                NodeId = nodeid,
                Id = id,
            };
            this.info = info;
        }

        public SerializableMasterDenyDelInfo(MasterDenyDelInfo info)
        {
            this.info = info;
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

            writer.WritePackable(new SerializableMasterDenyDelInfo(value));
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
