using System;

namespace linker.libs
{
    /// <summary>
    /// 序列化器
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public T Deserialize<T>(ReadOnlySpan<byte> buffer);
        /// <summary>
        /// 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public byte[] Serialize<T>(T value);
    }
}
