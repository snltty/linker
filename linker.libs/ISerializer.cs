using System;

namespace linker.libs
{
    public interface ISerializer
    {
        public T Deserialize<T>(ReadOnlySpan<byte> buffer);
        public byte[] Serialize<T>(T value);
    }
}
