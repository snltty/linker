using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.decenter
{
    public sealed class DecenterServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        /// <summary>
        /// name machineid data
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DecenterCacheInfo>> decenters = new ConcurrentDictionary<string, ConcurrentDictionary<string, DecenterCacheInfo>>();

        public DecenterServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
            ClearTask();
        }

        [MessengerId((ushort)DecenterMessengerIds.Push)]
        public void Push(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo signin) == false) return;
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);

            lock (decenters)
            {
                if (decenters.TryGetValue(info.Name, out ConcurrentDictionary<string, DecenterCacheInfo> dic) == false)
                {
                    dic = new ConcurrentDictionary<string, DecenterCacheInfo>();
                    decenters.TryAdd(info.Name, dic);
                }
                if (dic.TryGetValue(connection.Id, out DecenterCacheInfo cache) == false)
                {
                    cache = new DecenterCacheInfo();
                    dic.TryAdd(connection.Id, cache);
                }
                cache.Data = info.Data;
                cache.SignIn = signin;
                cache.Version.Increment();
            }

            Memory<byte> memory = serializer.Serialize(info);
            List<SignCacheInfo> caches = signCaching.Get(signin.GroupId).Where(c => c.MachineId != connection.Id && c.Connected).ToList();
            List<Task<bool>> tasks = caches.Select(c => sender.SendOnly(new MessageRequestWrap
            {
                Connection = c.Connection,
                MessengerId = (ushort)DecenterMessengerIds.Notify,
                Payload = memory
            })).ToList();
        }

        [MessengerId((ushort)DecenterMessengerIds.Pull)]
        public void Pull(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo signin) == false) return;

            string name = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (decenters.TryGetValue(name, out ConcurrentDictionary<string, DecenterCacheInfo> dic) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            IEnumerable<Memory<byte>> data = dic.Where(c => c.Key != connection.Id && c.Value.SignIn.GroupId == signin.GroupId).Select(c => c.Value.Data);
            connection.Write(serializer.Serialize(data));
        }

        [MessengerId((ushort)DecenterMessengerIds.PullPage)]
        public void PullPage(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo signin) == false) return;

            DecenterPullPageInfo page = serializer.Deserialize<DecenterPullPageInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (decenters.TryGetValue(page.Name, out ConcurrentDictionary<string, DecenterCacheInfo> dic) == false)
            {
                connection.Write(serializer.Serialize(new DecenterPullPageResultInfo { }));
                return;
            }

            IEnumerable<Memory<byte>> data = dic.Where(c => c.Key != connection.Id && c.Value.SignIn.GroupId == signin.GroupId).Select(c => c.Value.Data);
            connection.Write(serializer.Serialize(new DecenterPullPageResultInfo
            {
                Count = data.Count(),
                List = data.Skip((page.Page - 1) * page.Size).Take(page.Size).ToList(),
                Page = page.Page,
                Size = page.Size
            }));
        }

        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                List<string> removes = decenters.Values.SelectMany(c => c.Values).Where(c => c.SignIn.Connected == false).Select(c => c.SignIn.Id).ToList();
                foreach (ConcurrentDictionary<string, DecenterCacheInfo> dic in decenters.Values)
                {
                    foreach (string id in removes)
                    {
                        dic.TryRemove(id, out _);
                    }
                }
            }, 30000);
        }

        [MessengerId((ushort)DecenterMessengerIds.AddForward)]
        public void AddForward(IConnection connection)
        {
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                List<SignCacheInfo> caches = signCaching.Get(cache.GroupId).Where(c => c.MachineId != connection.Id && c.Connected).ToList();
                List<Task<MessageResponeInfo>> tasks = new List<Task<MessageResponeInfo>>();
                foreach (SignCacheInfo item in caches)
                {
                    tasks.Add(sender.SendReply(new MessageRequestWrap
                    {
                        Connection = item.Connection,
                        MessengerId = (ushort)DecenterMessengerIds.Add,
                        Payload = connection.ReceiveRequestWrap.Payload,
                        Timeout = 30000,
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
                        }, (ushort)DecenterMessengerIds.AddForward).ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                });
            }
        }


    }

    public sealed class DecenterClientMessenger : IMessenger
    {
        private readonly DecenterClientTransfer syncTreansfer;
        private readonly ISerializer serializer;

        public DecenterClientMessenger(DecenterClientTransfer syncTreansfer, ISerializer serializer)
        {
            this.syncTreansfer = syncTreansfer;
            this.serializer = serializer;
        }

        [MessengerId((ushort)DecenterMessengerIds.Notify)]
        public void Notify(IConnection connection)
        {
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            syncTreansfer.Notify(info);
        }

        [MessengerId((ushort)DecenterMessengerIds.Add)]
        public void Add(IConnection connection)
        {
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(syncTreansfer.Add(info));
        }

    }

    public sealed class DecenterCacheInfo
    {
        public SignCacheInfo SignIn { get; set; }
        public VersionManager Version { get; set; } = new VersionManager();
        public Memory<byte> Data { get; set; }
    }
}
