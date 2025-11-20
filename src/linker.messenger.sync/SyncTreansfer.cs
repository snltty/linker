using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;
using System.Xml.Linq;

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
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"add sync {string.Join(",", list.Select(c => c.GetType().Name))}");
            syncs = syncs.Concat(list).Distinct().ToList();
        }

        public List<string> GetNames()
        {
            return syncs.Select(c => c.Name).ToList();
        }

        public async Task Sync(string name, string[] ids, Memory<byte> data)
        {
            var sync = syncs.FirstOrDefault(c => c.Name == name);
            if (sync != null)
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)ConfigMessengerIds.Sync184Forward,
                    Payload = serializer.Serialize(new SyncInfo { Name = sync.Name, Data = data, Ids = ids }),
                }).ConfigureAwait(false);
            }
        }
        public async Task Sync(string[] names, string[] ids)
        {
            if (names.Length == 1)
            {
                var sync = syncs.FirstOrDefault(c => c.Name == names[0]);
                if (sync != null)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = signInClientState.Connection,
                        MessengerId = (ushort)ConfigMessengerIds.Sync184Forward,
                        Payload = serializer.Serialize(new SyncInfo { Name = sync.Name, Data = sync.GetData(), Ids = ids }),
                    }).ConfigureAwait(false);
                }
                return;
            }

            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync().ConfigureAwait(false);
                try
                {
                    var tasks = syncs.Where(c => names.Contains(c.Name)).Select(c =>
                     {
                         return messengerSender.SendOnly(new MessageRequestWrap
                         {
                             Connection = signInClientState.Connection,
                             MessengerId = (ushort)ConfigMessengerIds.Sync184Forward,
                             Payload = serializer.Serialize(new SyncInfo { Name = c.Name, Data = c.GetData(), Ids = ids }),

                         });
                     }).ToList();
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
                slim.Release();
            });
        }
        public void Sync(SyncInfo info)
        {
            ISync sync = syncs.FirstOrDefault(c => c.Name == info.Name);
            sync?.SetData(info.Data);
        }
    }
}