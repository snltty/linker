using linker.libs;
using MemoryPack;
using System.Diagnostics.CodeAnalysis;

namespace linker.messenger.serializer.memorypack
{
    public sealed class PlusMemoryPackSerializer : ISerializer
    {
        public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ReadOnlySpan<byte> buffer)
        {
            return MemoryPackSerializer.Deserialize<T>(buffer);
        }

        public byte[] Serialize<T>(T value)
        {
            return MemoryPackSerializer.Serialize(value);
        }

    }
}
