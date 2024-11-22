using linker.plugins.client;
using linker.plugins.messenger;
using MemoryPack;
using linker.libs;
using linker.plugins.decenter.messenger;

namespace linker.plugins.decenter
{

    [MemoryPackable]
    public sealed partial class DecenterSyncInfo
    {
        public DecenterSyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }

    public sealed class DecenterTransfer
    {
        private List<IDecenter> syncs = new List<IDecenter>();

        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        public DecenterTransfer(IMessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            SyncTask();
        }

        public void LoadDecenters(List<IDecenter> list)
        {
            syncs = list;
        }

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
                    var tasks = syncs.Where(c => c.DataVersion.Reset()).Select(c =>
                    {
                        return new DecenterSyncTaskInfo
                        {
                            Decenter = c,
                            Time = Environment.TickCount64,
                            Task = messengerSender.SendReply(new MessageRequestWrap
                            {
                                Connection = clientSignInState.Connection,
                                MessengerId = (ushort)DecenterMessengerIds.SyncForward,
                                Payload = MemoryPackSerializer.Serialize(new DecenterSyncInfo { Name = c.Name, Data = c.GetData() }),
                                Timeout = 15000
                            })
                        };
                    }).ToList();
                    await Task.WhenAll(tasks.Select(c => c.Task));
                    foreach (var task in tasks)
                    {
                        if (task.Task.Result.Code == MessageResponeCodes.OK)
                        {
                            List<ReadOnlyMemory<byte>> list = MemoryPackSerializer.Deserialize<List<ReadOnlyMemory<byte>>>(task.Task.Result.Data.Span);
                            task.Decenter.SetData(list);
                        }
                        else
                        {
                            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            {
                                LoggerHelper.Instance.Error($"decenter {task.Decenter.Name}->{task.Task.Result.Code}");
                            }
                        }
                        /*
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Debug($"decenter {task.Decenter.Name}->{Environment.TickCount64 - task.Time}ms");
                        }
                        */
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
            }, () => 3000);
        }

        class DecenterSyncTaskInfo
        {
            public IDecenter Decenter { get; set; }
            public Task<MessageResponeInfo> Task { get; set; }

            public long Time { get; set; }
        }
    }
}
