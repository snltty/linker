using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;

namespace linker.messenger.decenter
{
    public sealed class DecenterServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        public DecenterServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
        }

        [MessengerId((ushort)DecenterMessengerIds.SyncForward)]
        public void SyncForward(IConnection connection)
        {
            try
            {
                DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
                {
                    uint requiestid = connection.ReceiveRequestWrap.RequestId;

                    List<SignCacheInfo> caches = signCaching.Get(cache.GroupId);
                    List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                    foreach (SignCacheInfo item in caches.Where(c => c.MachineId != connection.Id && c.Connected))
                    {
                        tasks.Add(sender.SendReply(new MessageRequestWrap
                        {
                            Connection = item.Connection,
                            MessengerId = (ushort)DecenterMessengerIds.Sync,
                            Payload = connection.ReceiveRequestWrap.Payload,
                            Timeout = 5000,
                        }));
                    }

                    Task.WhenAll(tasks).ContinueWith(async (result) =>
                    {
                        try
                        {
                            List<ReadOnlyMemory<byte>> results = tasks.Where(c => c.Result.Code == MessageResponeCodes.OK).Select(c => c.Result.Data).ToList();
                            await sender.ReplyOnly(new MessageResponseWrap
                            {
                                RequestId = requiestid,
                                Connection = connection,
                                Payload = serializer.Serialize(results)
                            }, (ushort)DecenterMessengerIds.SyncForward).ConfigureAwait(false);

                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Instance.Error(ex);
                        }
                    });
                }
            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// 服务端的数据同步中转
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)DecenterMessengerIds.SyncForward170)]
        public async void SyncForward170(IConnection connection)
        {
            try
            {
                DecenterSyncInfo170 info = serializer.Deserialize<DecenterSyncInfo170>(connection.ReceiveRequestWrap.Payload.Span);

                if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
                {
                    //没有目标，就是通知的情况，发给所有在线客户端
                    if (string.IsNullOrWhiteSpace(info.ToMachineId))
                    {
                        TimerHelper.Async(async () =>
                        {
                            List<SignCacheInfo> onlineMachines = signCaching.Get(cache.GroupId).Where(c => c.MachineId != connection.Id && c.Connected).ToList();
                            //可能很多客户端，稍微休息休息，不要一次发太多
                            while (onlineMachines.Count > 0)
                            {
                                List<SignCacheInfo> onlineRange = onlineMachines.Take(10).ToList();
                                onlineMachines.RemoveRange(0, onlineRange.Count);

                                List<Task<bool>> tasks = onlineRange.Select(c => sender.SendOnly(new MessageRequestWrap
                                {
                                    Connection = c.Connection,
                                    MessengerId = (ushort)DecenterMessengerIds.Sync170,
                                    Payload = connection.ReceiveRequestWrap.Payload
                                })).ToList();

                                await Task.WhenAll(tasks);
                            }
                        });
                    }
                    //有目标，就是回复的情况，比如之前A通知了B，B回复一条自己的给A，设置了目标为A，就只需要发给A就行
                    else if (signCaching.TryGet(info.ToMachineId, out SignCacheInfo cacheTo))
                    {
                        await sender.SendOnly(new MessageRequestWrap
                        {
                            Connection = cacheTo.Connection,
                            MessengerId = (ushort)DecenterMessengerIds.Sync170,
                            Payload = connection.ReceiveRequestWrap.Payload
                        });
                    }

                }
            }
            catch (Exception)
            {
            }
        }
    }

    public sealed class DecenterClientMessenger : IMessenger
    {
        private readonly DecenterClientTransfer syncTreansfer;
        private readonly ISerializer serializer;
        private readonly IMessengerSender sender;
        private readonly SignInClientState signInClientState;

        public DecenterClientMessenger(DecenterClientTransfer syncTreansfer, ISerializer serializer, IMessengerSender sender, SignInClientState signInClientState)
        {
            this.syncTreansfer = syncTreansfer;
            this.serializer = serializer;
            this.sender = sender;
            this.signInClientState = signInClientState;
        }

        [MessengerId((ushort)DecenterMessengerIds.Sync)]
        public void Sync(IConnection connection)
        {
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(syncTreansfer.Sync(info));
        }

        [MessengerId((ushort)DecenterMessengerIds.Sync170)]
        public async void Sync170(IConnection connection)
        {
            DecenterSyncInfo170 info = serializer.Deserialize<DecenterSyncInfo170>(connection.ReceiveRequestWrap.Payload.Span);
            Memory<byte> memory = syncTreansfer.Sync(info);

            //群发来的，我就回复
            if (string.IsNullOrWhiteSpace(info.ToMachineId))
            {
                await sender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)DecenterMessengerIds.SyncForward170,
                    Payload = serializer.Serialize(new DecenterSyncInfo170
                    {
                        Data = memory,
                        FromMachineId = signInClientState.Connection.Id,
                        ToMachineId = info.FromMachineId,
                        Name = info.Name
                    })
                });
            }
        }
    }
}
