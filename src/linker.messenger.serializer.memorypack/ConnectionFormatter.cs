using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    /// <summary>
    ///  MemoryPack 的IConnection序列化扩展
    /// </summary>
    public sealed class ConnectionFormatter : MemoryPackFormatter<IConnection>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IConnection value)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref IConnection value)
        {
            if (!reader.TryReadCollectionHeader(out int len))
            {
                value = null;
                return;
            }
        }
    }
}
