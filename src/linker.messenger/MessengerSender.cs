using linker.libs;
using linker.libs.extends;
using System.Collections.Concurrent;

namespace linker.messenger
{
    /// <summary>
    /// 信标消息发送
    /// </summary>
    public interface IMessengerSender
    {
        /// <summary>
        /// 发送并等待回复
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task<MessageResponeInfo> SendReply(MessageRequestWrap msg);

        /// <summary>
        /// 仅发送
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task<bool> SendOnly(MessageRequestWrap msg);
        /// <summary>
        /// 仅回复
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="messengerId"></param>
        /// <returns></returns>
        public ValueTask<bool> ReplyOnly(MessageResponseWrap msg, ushort messengerId);
        /// <summary>
        /// 回复
        /// </summary>
        /// <param name="wrap"></param>
        public ushort Response(MessageResponseWrap wrap);
    }

    /// <summary>
    /// 消息发送器
    /// </summary>
    public class MessengerSender : IMessengerSender
    {
        public readonly NumberSpaceUInt32 requestIdNumberSpace = new NumberSpaceUInt32(0);
        private readonly ConcurrentDictionary<uint, ReplyWrapInfo> sends = new ConcurrentDictionary<uint, ReplyWrapInfo>();

        public MessengerSender()
        {
        }

        public virtual void Add(ushort id, long receiveBytes, long sendtBytes) { }
        public virtual void AddStopwatch(ushort id, long time, MessageTypes type) { }


        private void ResetRequestId(MessageRequestWrap msg)
        {
            if (msg.RequestId == 0)
            {
                uint id = msg.RequestId;
                Interlocked.CompareExchange(ref id, requestIdNumberSpace.Increment(), 0);
                msg.RequestId = id;
            }
        }
        private void ValidateTimeout(MessageRequestWrap msg)
        {
            if (msg.Timeout <= 0)
            {
                msg.Timeout = 15000;
            }
        }

        public async Task<MessageResponeInfo> SendReply(MessageRequestWrap msg)
        {
            msg.Reply = true;
            ResetRequestId(msg);
            ValidateTimeout(msg);

            if (await SendOnly(msg).ConfigureAwait(false) == false)
            {
                return new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT };
            }

            try
            {
                TaskCompletionSource<MessageResponeInfo> tcs = new TaskCompletionSource<MessageResponeInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
                sends.TryAdd(msg.RequestId, new ReplyWrapInfo { Tcs = tcs, MessengerId = msg.MessengerId });
                return await tcs.WithTimeout(msg.Timeout).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (sends.TryRemove(msg.RequestId, out ReplyWrapInfo info))
                    AddStopwatch(info.MessengerId, Environment.TickCount64 - info.SendTime, MessageTypes.REQUEST);
                return new MessageResponeInfo { Code = MessageResponeCodes.TIMEOUT };
            }
        }
        public async Task<bool> SendOnly(MessageRequestWrap msg)
        {
            if (msg.Connection == null || msg.Connection.Connected == false)
            {
                return false;
            }

            ResetRequestId(msg);
            byte[] bytes = msg.ToArray(out int length);
            Add(msg.MessengerId, 0, bytes.Length);

            try
            {
                return await msg.Connection.SendAsync(bytes.AsMemory(0, length)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                msg.Return(bytes);
            }
            return false;
        }

        public async ValueTask<bool> ReplyOnly(MessageResponseWrap msg, ushort messengerId)
        {
            if (msg.Connection == null)
            {
                return false;
            }

            try
            {

                byte[] bytes = msg.ToArray(out int length);

                Add(messengerId, 0, length);

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

        public ushort Response(MessageResponseWrap wrap)
        {
            if (sends.TryRemove(wrap.RequestId, out ReplyWrapInfo info))
            {
                byte[] bytes = new byte[wrap.Payload.Length];
                wrap.Payload.CopyTo(bytes);

                Add(info.MessengerId, bytes.Length, 0);
                AddStopwatch(info.MessengerId, Environment.TickCount64 - info.SendTime, MessageTypes.REQUEST);
                info.Tcs.TrySetResult(new MessageResponeInfo { Code = wrap.Code, Data = bytes, Connection = wrap.Connection });
                return info.MessengerId;
            }
            return 0;
        }
    }

    public sealed class ReplyWrapInfo
    {
        public TaskCompletionSource<MessageResponeInfo> Tcs { get; set; }
        public ushort MessengerId { get; set; }
        public long SendTime { get; set; } = Environment.TickCount64;
    }
    public sealed class MessageResponeInfo
    {
        public MessageResponeCodes Code { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }
        public IConnection Connection { get; set; }

    }
}

