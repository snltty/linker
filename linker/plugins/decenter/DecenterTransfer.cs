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
                    foreach (var sync in syncs)
                    {
                        if (sync.DataVersion.Reset())
                        {
                            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                            {
                                Connection = clientSignInState.Connection,
                                MessengerId = (ushort)DecenterMessengerIds.SyncForward,
                                Payload = MemoryPackSerializer.Serialize(new DecenterSyncInfo { Name = sync.Name, Data = sync.GetData() })
                            });
                            if (resp.Code == MessageResponeCodes.OK)
                            {
                                sync.SetData(MemoryPackSerializer.Deserialize<List<ReadOnlyMemory<byte>>>(resp.Data.Span));
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
            }, () => 3000);
        }
    }
}
