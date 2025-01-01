using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.sync
{
    public sealed class SyncTreansfer
    {
        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        private List<ISync> syncs = new List<ISync>();

        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISerializer serializer;
        public SyncTreansfer(IMessengerSender messengerSender, SignInClientState signInClientState, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.serializer = serializer;
        }

        public void AddSyncs(List<ISync> list)
        {
            LoggerHelper.Instance.Info($"add sync {string.Join(",", list.Select(c => c.GetType().Name))}");
            syncs = syncs.Concat(list).Distinct().ToList();
        }

        public List<string> GetNames()
        {
            return syncs.Select(c => c.Name).ToList();
        }

        public void Sync(string[] names)
        {
            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();
                try
                {
                    var tasks = syncs.Where(c => names.Contains(c.Name)).Select(c =>
                     {
                         return messengerSender.SendOnly(new MessageRequestWrap
                         {
                             Connection = signInClientState.Connection,
                             MessengerId = (ushort)ConfigMessengerIds.SyncForward,
                             Payload = serializer.Serialize(new SyncInfo { Name = c.Name, Data = c.GetData() }),

                         });
                     }).ToList();
                    await Task.WhenAll(tasks);
                }
                catch (Exception)
                {
                }
                slim.Release();
            });
        }
        public async Task Sync(string name)
        {
            var sync = syncs.FirstOrDefault(c => c.Name == name);
            if(sync != null)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)ConfigMessengerIds.SyncForward,
                    Payload = serializer.Serialize(new SyncInfo { Name = sync.Name, Data = sync.GetData() }),
                });
            }
        }
        public void Sync(SyncInfo info)
        {
            var sync = syncs.FirstOrDefault(c => c.Name == info.Name);
            if (sync != null)
            {
                sync.SetData(info.Data);
            }
        }
    }
}