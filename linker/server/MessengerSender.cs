using linker.libs;
using System.Collections.Concurrent;

namespace linker.server
{
    /// <summary>
    /// 消息发送器
    /// </summary>
    public sealed class MessengerSender
    {
        public NumberSpaceUInt32 requestIdNumberSpace = new NumberSpaceUInt32(0);
        private ConcurrentDictionary<uint, TaskCompletionSource<MessageResponeInfo>> sends = new ConcurrentDictionary<uint, TaskCompletionSource<MessageResponeInfo>>();

        public MessengerSender()
        {
        }

        /// <summary>
        /// 发送并等待回复
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> SendReply(MessageRequestWrap msg)
        {
            if (msg.Connection == null || msg.Connection.Connected == false)
            {
                return new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT };
            }

            if (msg.RequestId == 0)
            {
                uint id = msg.RequestId;
                Interlocked.CompareExchange(ref id, requestIdNumberSpace.Increment(), 0);
                msg.RequestId = id;
            }

            msg.Reply = true;
            if (msg.Timeout <= 0)
            {
                msg.Timeout = 15000;
            }
            TaskCompletionSource<MessageResponeInfo> tcs = new TaskCompletionSource<MessageResponeInfo>();
            sends.TryAdd(msg.RequestId, tcs);

            bool res = await SendOnly(msg).ConfigureAwait(false);
            if (res == false)
            {
                sends.TryRemove(msg.RequestId, out _);
                tcs.SetResult(new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT });
            }

            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            catch (Exception)
            {
                return new MessageResponeInfo { Code = MessageResponeCodes.TIMEOUT };
            }
        }

        /// <summary>
        /// 只发送，不等回复
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> SendOnly(MessageRequestWrap msg)
        {
            if (msg.Connection == null || msg.Connection.Connected == false)
            {
                return false;
            }

            try
            {
                if (msg.RequestId == 0)
                {
                    uint id = msg.RequestId;
                    Interlocked.CompareExchange(ref id, requestIdNumberSpace.Increment(), 0);
                    msg.RequestId = id;
                }

                byte[] bytes = msg.ToArray(out int length);
                bool res = await msg.Connection.SendAsync(bytes.AsMemory(0, length)).ConfigureAwait(false);
                msg.Return(bytes);
                return res;
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            return false;
        }

        /// <summary>
        /// 回复远程消息，收到某个连接的消息后，通过这个再返回消息给它
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async ValueTask<bool> ReplyOnly(MessageResponseWrap msg)
        {
            if (msg.Connection == null)
            {
                return false;
            }

            try
            {
                byte[] bytes = msg.ToArray(out int length);
                bool res = await msg.Connection.SendAsync(bytes.AsMemory(0, length)).ConfigureAwait(false);
                msg.Return(bytes);
                return res;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            return false;
        }
        /// <summary>
        /// 回复本地消息，发送消息后，socket收到消息，通过这个方法回复给刚刚发送的对象
        /// </summary>
        /// <param name="wrap"></param>
        public void Response(MessageResponseWrap wrap)
        {
            if (sends.TryRemove(wrap.RequestId, out TaskCompletionSource<MessageResponeInfo> tcs))
            {
                byte[] data = new byte[wrap.Payload.Length];
                wrap.Payload.CopyTo(data);
                tcs.SetResult(new MessageResponeInfo { Code = wrap.Code, Data = data });
            }
        }
    }

    public sealed class MessageResponeInfo
    {
        public MessageResponeCodes Code { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }
    }
}

