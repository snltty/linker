using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.wakeup
{
    /// <summary>
    /// 唤醒客户端
    /// </summary>
    public sealed class WakeupClientMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly WakeupTransfer wakeupTransfer;
        public WakeupClientMessenger(IMessengerSender messengerSender, ISerializer serializer, WakeupTransfer wakeupTransfer)
        {
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.wakeupTransfer = wakeupTransfer;
        }

        [MessengerId((ushort)WakeupMessengerIds.Get)]
        public void Get(IConnection connection)
        {
            WakeupSearchInfo info = serializer.Deserialize<WakeupSearchInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(wakeupTransfer.Get(info)));
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.Add)]
        public void Add(IConnection connection)
        {
            WakeupInfo info = serializer.Deserialize<WakeupInfo>(connection.ReceiveRequestWrap.Payload.Span);
            wakeupTransfer.Add(info);
        }
        // <summary>
        /// 删除
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.Remove)]
        public void Remove(IConnection connection)
        {
            string id = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            wakeupTransfer.Remove(id);
        }

        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.Send)]
        public void Send(IConnection connection)
        {
            WakeupSendInfo info = serializer.Deserialize<WakeupSendInfo>(connection.ReceiveRequestWrap.Payload.Span);
            _ = wakeupTransfer.Send(info);
        }

        /// <summary>
        /// 获取com列表
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.Coms)]
        public void Coms(IConnection connection)
        {
            connection.Write(serializer.Serialize(wakeupTransfer.ComNames()));
        }
        /// <summary>
        /// 获取hid列表
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.Hids)]
        public void Hids(IConnection connection)
        {
            connection.Write(serializer.Serialize(wakeupTransfer.HidIds()));
        }
    }

    /// <summary>
    /// 唤醒服务端
    /// </summary>
    public sealed class WakeupServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        public WakeupServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        /// <summary>
        /// 获取对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.GetForward)]
        public void GetForward(IConnection connection)
        {
            WakeupSearchForwardInfo info = serializer.Deserialize<WakeupSearchForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)WakeupMessengerIds.Get,
                    Payload = serializer.Serialize(info.Data)
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)WakeupMessengerIds.GetForward).ConfigureAwait(false);
                    }
                });
            }
            else
            {
                connection.Write(serializer.Serialize(new List<WakeupInfo>()));
            }
        }
        /// <summary>
        /// 添加对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.AddForward)]
        public async Task AddForward(IConnection connection)
        {
            WakeupAddForwardInfo info = serializer.Deserialize<WakeupAddForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)WakeupMessengerIds.Add,
                    Payload = serializer.Serialize(info.Data)
                }).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// 删除对端的记录
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.RemoveForward)]
        public async Task RemoveForward(IConnection connection)
        {
            WakeupRemoveForwardInfo info = serializer.Deserialize<WakeupRemoveForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)WakeupMessengerIds.Remove,
                    Payload = serializer.Serialize(info.Id)
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 命令转发
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)WakeupMessengerIds.SendForward)]
        public async Task SendForward(IConnection connection)
        {
            WakeupSendForwardInfo info = serializer.Deserialize<WakeupSendForwardInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)WakeupMessengerIds.Send,
                    Payload = serializer.Serialize(info.Data)
                }).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// 获取com列表转发
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.ComsForward)]
        public void ComsForward(IConnection connection)
        {
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)WakeupMessengerIds.Coms,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)WakeupMessengerIds.ComsForward).ConfigureAwait(false);
                    }
                });
            }
            else
            {
                connection.Write(serializer.Serialize(Array.Empty<string>()));
            }
        }

        /// <summary>
        /// 获取hid列表转发
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)WakeupMessengerIds.HidsForward)]
        public void HidsForward(IConnection connection)
        {
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)WakeupMessengerIds.Hids,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data,
                            RequestId = requestid
                        }, (ushort)WakeupMessengerIds.HidsForward).ConfigureAwait(false);
                    }
                });
            }
            else
            {
                connection.Write(serializer.Serialize(Array.Empty<string>()));
            }
        }
    }
}
