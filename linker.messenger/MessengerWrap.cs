using linker.libs.extends;
using System.Buffers;
using System.ComponentModel;

namespace linker.messenger
{
    /// <summary>
    /// 请求数据包，只填  MessengerId  Payload Connection 就可以，Timeout可选，其它不用填
    /// </summary>
    public sealed class MessageRequestWrap
    {
        /// <summary>
        /// 超时ms,默认15000
        /// </summary>
        public int Timeout { get; set; }
        /// <summary>
        /// 消息id
        /// </summary>
        public ushort MessengerId { get; set; }
        /// <summary>
        /// 请求id，不用填
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 是否需要回复
        /// </summary>
        public bool Reply { get; internal set; }
        /// <summary>
        /// 荷载
        /// </summary>
        public ReadOnlyMemory<byte> Payload { get; set; }
        /// <summary>
        /// 连接对象
        /// </summary>
        public IConnection Connection { get; set; }

        /// <summary>
        /// 序列化，使用了池化，用完后记得调用 Return
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] ToArray(out int length)
        {
            int index = 0;

            length = 4
                + 1 //Reply + type
                + 4
                + 2
                + Payload.Length;

            byte[] res = ArrayPool<byte>.Shared.Rent(length);

            ((uint)length - 4).ToBytes(res);
            index += 4;

            res[index] = (byte)((Reply ? 1 : 0) << 4 | (byte)MessageTypes.REQUEST);
            index += 1;

            RequestId.ToBytes(res.AsMemory(index));
            index += 4;

            MessengerId.ToBytes(res.AsMemory(index));
            index += 2;

            Payload.CopyTo(res.AsMemory(index, Payload.Length));
            index += Payload.Length;

            return res;
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="memory"></param>
        public unsafe void FromArray(ReadOnlyMemory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            Reply = span[index] >> 4 == 1;
            index += 1;


            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            MessengerId = span.Slice(index).ToUInt16();
            index += 2;

            Payload = memory.Slice(index);
        }
        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

    }

    /// <summary>
    /// 回执消息包
    /// </summary>
    public sealed class MessageResponseWrap
    {
        /// <summary>
        /// 连接对象
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 返回码
        /// </summary>
        public MessageResponeCodes Code { get; set; }
        /// <summary>
        /// 消息id
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 何在
        /// </summary>
        public ReadOnlyMemory<byte> Payload { get; set; }

        /// <summary>
        /// 序列化。用了池化，用完记得 Return
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray(out int length)
        {
            length = 4
                + 1 //type
                + 1 //code
                + 4 //requestid
                + Payload.Length;

            byte[] res = ArrayPool<byte>.Shared.Rent(length);

            int index = 0;
            ((uint)length - 4).ToBytes(res);
            index += 4;

            res[index] = (byte)MessageTypes.RESPONSE;
            index += 1;

            res[index] = (byte)Code;
            index += 1;

            RequestId.ToBytes(res.AsMemory(index));
            index += 4;

            if (Payload.Length > 0)
            {
                Payload.CopyTo(res.AsMemory(index, Payload.Length));
                index += Payload.Length;
            }
            return res;
        }
        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="memory"></param>
        public void FromArray(ReadOnlyMemory<byte> memory)
        {
            var span = memory.Span;
            int index = 0;

            index += 1;

            Code = (MessageResponeCodes)span[index];
            index += 1;

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            Payload = memory.Slice(index);
        }

        public void Return(byte[] array)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    /// <summary>
    /// 消息状态
    /// </summary>
    [Flags]
    public enum MessageResponeCodes : byte
    {
        [Description("成功")]
        OK = 0,
        [Description("网络未连接")]
        NOT_CONNECT = 1,
        [Description("网络资源未找到")]
        NOT_FOUND = 2,
        [Description("网络超时")]
        TIMEOUT = 3,
        [Description("程序错误")]
        ERROR = 4,
    }

    /// <summary>
    /// 消息类别
    /// </summary>
    [Flags]
    public enum MessageTypes : byte
    {
        [Description("请求")]
        REQUEST = 0,
        [Description("回复")]
        RESPONSE = 1
    }

}

