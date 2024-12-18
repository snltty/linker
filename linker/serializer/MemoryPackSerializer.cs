using linker.libs;
using MemoryPack;
using System.Diagnostics.CodeAnalysis;

namespace linker.serializer
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
