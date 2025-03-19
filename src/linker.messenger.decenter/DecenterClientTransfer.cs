using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;

namespace linker.messenger.decenter
{
    public sealed class DecenterClientTransfer
    {
        private List<IDecenter> syncs = new List<IDecenter>();

        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISerializer serializer;
        public DecenterClientTransfer(IMessengerSender messengerSender, SignInClientState signInClientState, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.serializer = serializer;


            SyncTask();
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="list"></param>
        public void AddDecenters(List<IDecenter> list)
        {
            LoggerHelper.Instance.Info($"add decenter {string.Join(",", list.Select(c => c.GetType().Name))}");
            syncs = syncs.Concat(list).Distinct().ToList();
        }

        /// <summary>
        /// 同步
        /// </summary>
        /// <param name="decenterSyncInfo"></param>
        /// <returns></returns>
        public Memory<byte> Sync(DecenterSyncInfo decenterSyncInfo)
        {
            IDecenter sync = syncs.FirstOrDefault(c => c.Name == decenterSyncInfo.Name);
            if (sync != null)
            {
                sync.SetData(decenterSyncInfo.Data);
                return sync.GetData();
            }
            return Helper.EmptyArray;
        }
        /// <summary>
        /// 通知
        /// </summary>
        /// <param name="decenterSyncInfo"></param>
        public void Notify(DecenterSyncInfo decenterSyncInfo)
        {
            IDecenter sync = syncs.FirstOrDefault(c => c.Name == decenterSyncInfo.Name);
            if (sync != null)
            {
                sync.SetData(decenterSyncInfo.Data);
            }
        }

        private void SyncTask()
        {
            signInClientState.NetworkFirstEnabledHandle += () =>
            {
                foreach (IDecenter item in syncs)
                {
                    item.SyncVersion.Add();
                }
            };
            TimerHelper.SetIntervalLong(async () =>
            {
                if (signInClientState.Connected == false) return;
                try
                {
                    List<IDecenter> updates = syncs.Where(c => c.SyncVersion.Reset()).ToList();
                    if (updates.Any())
                    {
                        await Task.WhenAll(updates.Select(c =>
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                LoggerHelper.Instance.Debug($"decenter push {c.Name}");

                            return messengerSender.SendOnly(new MessageRequestWrap
                            {
                                Connection = signInClientState.Connection,
                                MessengerId = (ushort)DecenterMessengerIds.Push,
                                Payload = serializer.Serialize(new DecenterSyncInfo { Name = c.Name, Data = c.GetData() }),
                            });
                        }).ToList()).ConfigureAwait(false);

                        List<DecenterSyncTaskInfo> pullTasks = updates.Select(c =>
                        {
                            return new DecenterSyncTaskInfo
                            {
                                Decenter = c,
                                Time = Environment.TickCount64,
                                Task = messengerSender.SendReply(new MessageRequestWrap
                                {
                                    Connection = signInClientState.Connection,
                                    MessengerId = (ushort)DecenterMessengerIds.Pull,
                                    Payload = serializer.Serialize(c.Name),
                                    Timeout = 60000
                                })
                            };
                        }).ToList();
                        MessageResponeInfo[] pulls = await Task.WhenAll(pullTasks.Select(c => c.Task)).ConfigureAwait(false);
                        foreach (var task in pullTasks.Where(c => c.Task.Result.Code == MessageResponeCodes.OK && c.Task.Result.Data.Span.SequenceEqual(Helper.FalseArray) == false))
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                LoggerHelper.Instance.Debug($"decenter pull {task.Decenter.Name}");
                            List<ReadOnlyMemory<byte>> list = serializer.Deserialize<List<ReadOnlyMemory<byte>>>(task.Task.Result.Data.Span);
                            task.Decenter.SetData(list);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }, 300);
        }

        class DecenterSyncTaskInfo
        {
            public IDecenter Decenter { get; set; }
            public Task<MessageResponeInfo> Task { get; set; }
            public long Time { get; set; }
        }
    }
}
