using linker.tunnel.connection;
using MemoryPack;

namespace linker.messenger.serializer.memorypack
{
    /// <summary>
    ///  MemoryPack 的ITunnelConnection序列化扩展
    /// </summary>
    public sealed class TunnelConnectionFormatter : MemoryPackFormatter<ITunnelConnection>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ITunnelConnection value)
        {
            writer.WriteNullCollectionHeader();
            return;
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref ITunnelConnection value)
        {
            if (!reader.TryReadCollectionHeader(out int len))
            {
                value = null;
                return;
            }
        }
    }
}
