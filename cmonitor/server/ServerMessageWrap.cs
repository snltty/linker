using common.libs.extends;
using System.Buffers;
using System.ComponentModel;

namespace cmonitor.server
{
    public sealed class MessageRequestWrap
    {
        public int Timeout { get; set; }
        public ushort MessengerId { get; set; }
        public uint RequestId { get; set; }
        public bool Reply { get; internal set; }
        public Memory<byte> Payload { get; set; }

        public IConnection Connection { get; set; }

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
        public unsafe void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            Reply = span[index] >> 4 == 1;
            index += 1;


            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            MessengerId = span.Slice(index).ToUInt16();
            index += 2;

            Payload = memory.Slice(index, memory.Length - index);
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
        /// 
        /// </summary>
        public IConnection Connection { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public MessageResponeCodes Code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public uint RequestId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Memory<byte> Payload { get; set; }

        /// <summary>
        /// 转包
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
        public void FromArray(Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 0;

            index += 1;

            Code = (MessageResponeCodes)span[index];
            index += 1;

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            Payload = memory.Slice(index, memory.Length - index);
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

