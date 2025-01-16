using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.decenter
{
    public sealed partial class DecenterSyncInfo
    {
        public DecenterSyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }

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

        private void SyncTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                try
                {
                    var tasks = syncs.Where(c => c.SyncVersion.Reset()).Select(c =>
                    {
                        return new DecenterSyncTaskInfo
                        {
                            Decenter = c,
                            Time = Environment.TickCount64,
                            Task = messengerSender.SendReply(new MessageRequestWrap
                            {
                                Connection = signInClientState.Connection,
                                MessengerId = (ushort)DecenterMessengerIds.SyncForward,
                                Payload = serializer.Serialize(new DecenterSyncInfo { Name = c.Name, Data = c.GetData() }),
                                Timeout = 15000
                            })
                        };
                    }).ToList();
                    if (tasks.Count > 0)
                    {
                        await Task.WhenAll(tasks.Select(c => c.Task));
                        foreach (var task in tasks)
                        {
                            if (task.Task.Result.Code == MessageResponeCodes.OK)
                            {
                                List<ReadOnlyMemory<byte>> list = serializer.Deserialize<List<ReadOnlyMemory<byte>>>(task.Task.Result.Data.Span);
                                task.Decenter.SetData(list);
                            }
                            else
                            {
                                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                {
                                    LoggerHelper.Instance.Error($"decenter {task.Decenter.Name}->{task.Task.Result.Code}");
                                }
                            }
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
                return true;
            }, () => 300);
        }

        class DecenterSyncTaskInfo
        {
            public IDecenter Decenter { get; set; }
            public Task<MessageResponeInfo> Task { get; set; }

            public long Time { get; set; }
        }
    }
}
