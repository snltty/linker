using linker.libs;
using linker.plugins.client;
using linker.plugins.config.messenger;
using linker.plugins.messenger;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;

namespace linker.plugins.config
{
    public interface IConfigSync
    {
        public string Name { get; }
        public Memory<byte> GetData();
        public void SetData(Memory<byte> data);
    }

    [MemoryPackable]
    public sealed partial class ConfigAsyncInfo
    {
        public ConfigAsyncInfo() { }
        public string Name { get; set; }
        public Memory<byte> Data { get; set; }
    }

    public sealed partial class ConfigSyncTreansfer
    {
        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        private List<IConfigSync> syncs = new List<IConfigSync>();

        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        public ConfigSyncTreansfer(ServiceProvider serviceProvider, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;

            IEnumerable<Type> types = GetSourceGeneratorTypes();
            syncs = types.Select(c => (IConfigSync)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();
            LoggerHelper.Instance.Info($"load config sync transport:{string.Join(",", syncs.Select(c => c.Name))}");
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
                             Connection = clientSignInState.Connection,
                             MessengerId = (ushort)ConfigMessengerIds.SyncForward,
                             Payload = MemoryPackSerializer.Serialize(new ConfigAsyncInfo { Name = c.Name, Data = c.GetData() }),

                         });
                     });
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
                    Connection = clientSignInState.Connection,
                    MessengerId = (ushort)ConfigMessengerIds.SyncForward,
                    Payload = MemoryPackSerializer.Serialize(new ConfigAsyncInfo { Name = sync.Name, Data = sync.GetData() }),
                });
            }
        }
        public void Sync(ConfigAsyncInfo info)
        {
            var sync = syncs.FirstOrDefault(c => c.Name == info.Name);
            if (sync != null)
            {
                sync.SetData(info.Data);
            }
        }
    }
}