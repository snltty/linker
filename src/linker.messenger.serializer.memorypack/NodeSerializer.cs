using linker.messenger.node;
using MemoryPack;

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
        string SystemId => info.SystemId;

        [MemoryPackConstructor]
        SerializableNodeShareInfo(string nodeId, string name, string host, string systemid)
        {
            var info = new NodeShareInfo
            {
                NodeId = nodeId,
                Host = host,
                Name = name,
                SystemId = systemid
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
            value.SystemId = reader.ReadValue<string>();
        }
    }


}
