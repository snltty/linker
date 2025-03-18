using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;

namespace linker.messenger.decenter
{
    public partial class DecenterSyncInfo
    {
        public DecenterSyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }

    public sealed partial class DecenterSyncInfo170: DecenterSyncInfo
    {
        public DecenterSyncInfo170() { }
        public string FromMachineId { get; set; }
        public string ToMachineId { get; set; }
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
        public Memory<byte> Sync170(DecenterSyncInfo170 decenterSyncInfo)
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
            TimerHelper.SetIntervalLong(async () =>
            {
                if (signInClientState.Connected)
                {
                    try
                    {
                        IEnumerable<DecenterSyncTaskInfo> tasks = syncs.Where(c => c.SyncVersion.Reset()).Select(c =>
                        {
                            return new DecenterSyncTaskInfo
                            {
                                Decenter = c,
                                Time = Environment.TickCount64,
                                Task = messengerSender.SendOnly(new MessageRequestWrap
                                {
                                    Connection = signInClientState.Connection,
                                    MessengerId = (ushort)DecenterMessengerIds.SyncForward170,
                                    Payload = serializer.Serialize(new DecenterSyncInfo170 { FromMachineId = signInClientState.Connection.Id, Name = c.Name, Data = c.GetData() })
                                })
                            };
                        }).ToList();
                        if (tasks.Any())
                        {
                            await Task.WhenAll(tasks.Select(c => c.Task).ToList()).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                        {
                            LoggerHelper.Instance.Error(ex);
                        }
                    }
                }
            }, 300);
        }

        class DecenterSyncTaskInfo
        {
            public IDecenter Decenter { get; set; }
            public Task<bool> Task { get; set; }

            public long Time { get; set; }
        }
    }
}
