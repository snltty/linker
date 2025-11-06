using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;

namespace linker.messenger.decenter
{
    public sealed class DecenterClientTransfer
    {
        private StringChangedManager stringChangedManager = new StringChangedManager();
        private VersionMultipleManager versionMultipleManager = new VersionMultipleManager();
        private OperatingMultipleManager operatingMultipleManager = new OperatingMultipleManager();

        private List<IDecenter> decenters = new List<IDecenter>();

        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;
        public DecenterClientTransfer(IMessengerSender messengerSender, SignInClientState signInClientState, ISerializer serializer, ISignInClientStore signInClientStore)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;

            SyncTask();
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="list"></param>
        public void AddDecenters(List<IDecenter> list)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add decenter {string.Join(",", list.Select(c => c.GetType().Name))}");
            decenters = decenters.Concat(list).Distinct().ToList();
        }

        /// <summary>
        /// 同步
        /// </summary>
        /// <param name="decenterSyncInfo"></param>
        /// <returns></returns>
        public Memory<byte> Add(DecenterSyncInfo decenterSyncInfo)
        {
            IDecenter sync = decenters.FirstOrDefault(c => c.Name == decenterSyncInfo.Name);
            if (sync != null)
            {
                sync.AddData(decenterSyncInfo.Data);
                sync.DataVersion.Increment();
                versionMultipleManager.Increment(sync.Name);
                return GetData(sync);
            }
            return Helper.EmptyArray;
        }
        /// <summary>
        /// 通知
        /// </summary>
        /// <param name="decenterSyncInfo"></param>
        public void Notify(DecenterSyncInfo decenterSyncInfo)
        {
            IDecenter sync = decenters.FirstOrDefault(c => c.Name == decenterSyncInfo.Name);
            if (sync != null)
            {
                sync.AddData(decenterSyncInfo.Data);
                sync.DataVersion.Increment();
                versionMultipleManager.Increment(sync.Name);
            }
        }

        private Memory<byte> GetData(IDecenter decenter)
        {
            Memory<byte> data = decenter.GetData();
            decenter.AddData(data);
            decenter.DataVersion.Increment();
            return data;
        }

        private void ClearData()
        {
            if (stringChangedManager.Input(signInClientStore.Group.Id))
            {
                foreach (IDecenter item in decenters)
                {
                    item.ClearData();
                    item.PushVersion.Increment();
                }
            }
        }
        private void ProcData()
        {
            foreach (IDecenter item in decenters)
            {
                if (operatingMultipleManager.StartOperation(item.Name))
                {
                    if (versionMultipleManager.HasValueChange(item.Name))
                    {
                        Task.Run(item.ProcData).ContinueWith((result) => { operatingMultipleManager.StopOperation(item.Name); });
                    }
                    else
                    {
                        operatingMultipleManager.StopOperation(item.Name);
                    }
                }
            }
        }
        private async Task CheckData()
        {
            foreach (IDecenter item in decenters)
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)DecenterMessengerIds.Check,
                    Payload = serializer.Serialize(item.Name),
                });
                if (resp.Code != MessageResponeCodes.OK || resp.Data.Span.SequenceEqual(Helper.TrueArray) == false)
                {
                    item.PushVersion.Increment();
                }
            }
        }
        private async Task SyncData()
        {
            List<IDecenter> updates = decenters.Where(c => c.PushVersion.Restore()).ToList();
            if (updates.Count == 0) return;

            await Task.WhenAll(updates.Select(c =>
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"decenter push {c.Name}");

                return messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)DecenterMessengerIds.Push,
                    Payload = serializer.Serialize(new DecenterSyncInfo { Name = c.Name, Data = GetData(c) }),
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
                        Payload = serializer.Serialize(new KeyValuePair<string, bool>(c.Name, c.Force)),
                        Timeout = 3000
                    })
                };
            }).ToList();
            MessageResponeInfo[] pulls = await Task.WhenAll(pullTasks.Select(c => c.Task)).ConfigureAwait(false);
            foreach (var task in pullTasks.Where(c => c.Task.Result.Code == MessageResponeCodes.OK && c.Task.Result.Data.Span.SequenceEqual(Helper.FalseArray) == false))
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"decenter pull {task.Decenter.Name}->{task.Task.Result.Data.Length}");
                List<ReadOnlyMemory<byte>> list = serializer.Deserialize<List<ReadOnlyMemory<byte>>>(task.Task.Result.Data.Span);
                task.Decenter.AddData(list);
                task.Decenter.DataVersion.Increment();
                versionMultipleManager.Increment(task.Decenter.Name);
            }
            foreach (var task in pullTasks.Where(c => c.Task.Result.Code != MessageResponeCodes.OK || c.Task.Result.Data.Span.SequenceEqual(Helper.FalseArray)))
            {
                task.Decenter.PushVersion.Increment();
            }

        }

        private void SyncTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                if (signInClientState.Connected == false) return;
                try
                {
                    await CheckData();
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }, 30000);

            TimerHelper.SetIntervalLong(async () =>
            {
                if (signInClientState.Connected == false) return;
                try
                {
                    ClearData();
                    ProcData();
                    await SyncData().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
            }, 300);

            signInClientState.OnSignInSuccess += (times) =>
            {
                foreach (IDecenter item in decenters)
                {
                    item.PushVersion.Increment();
                }
            };
        }

        class DecenterSyncTaskInfo
        {
            public IDecenter Decenter { get; set; }
            public Task<MessageResponeInfo> Task { get; set; }
            public long Time { get; set; }
        }
    }
}
