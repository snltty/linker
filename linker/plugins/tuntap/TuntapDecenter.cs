using linker.libs;
using linker.plugins.client;
using linker.messenger.decenter;
using linker.plugins.tuntap.config;
using linker.serializer;
using System.Collections.Concurrent;
using linker.messenger.signin;

namespace linker.plugins.tuntap
{
    public sealed class TuntapDecenter : IDecenter
    {
        public string Name => "tuntap";
        public VersionManager SyncVersion { get; } = new VersionManager();

        public VersionManager DataVersion { get; } = new VersionManager();
        public ConcurrentDictionary<string, TuntapInfo> Infos => tuntapInfos;

        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        private readonly ConcurrentDictionary<string, TuntapInfo> tuntapInfos = new ConcurrentDictionary<string, TuntapInfo>();
       
        public Action OnChangeBefore { get; set; } = () => { };
        public Action OnChangeAfter { get; set; } = () => { };
        public Action OnReset { get; set; } = () => { };

        public Func<TuntapInfo> HandleCurrentInfo { get; set; } = () => { return null; };

        private readonly ISignInClientStore signInClientStore;
        public TuntapDecenter(ISignInClientStore signInClientStore, SignInClientState signInClientState)
        {
            this.signInClientStore = signInClientStore;
            signInClientState.NetworkEnabledHandle += NetworkEnable;
        }
        string groupid = string.Empty;
        private void NetworkEnable(int times)
        {
            if (groupid != signInClientStore.Group.Id)
            {
                tuntapInfos.Clear();
                OnReset();
            }
            groupid = signInClientStore.Group.Id;
        }

        public void Refresh()
        {
            SyncVersion.Add();
        }

        public Memory<byte> GetData()
        {
            TuntapInfo info = HandleCurrentInfo();
            tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            DataVersion.Add();
            return Serializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            TuntapInfo info = Serializer.Deserialize<TuntapInfo>(data.Span);
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Debug($"tuntap got {info.IP}");
            }

            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();
                try
                {
                    OnChangeBefore();
                    tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
                    DataVersion.Add();
                    OnChangeAfter();
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
                slim.Release();
            });
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<TuntapInfo> list = data.Select(c => Serializer.Deserialize<TuntapInfo>(c.Span)).ToList();
            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();

                try
                {
                    OnChangeBefore();
                    foreach (var item in list)
                    {
                        tuntapInfos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                        item.LastTicks.Update();
                    }
                    var removes = tuntapInfos.Keys.Except(list.Select(c => c.MachineId)).Where(c => c != signInClientStore.Id).ToList();
                    foreach (var item in removes)
                    {
                        if (tuntapInfos.TryGetValue(item, out TuntapInfo tuntapInfo))
                        {
                            tuntapInfo.Status = TuntapStatus.Normal;
                            tuntapInfo.LastTicks.Clear();
                        }
                    }
                    DataVersion.Add();
                    OnChangeAfter();
                }
                catch (Exception ex)
                {
                    if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        LoggerHelper.Instance.Error(ex);
                    }
                }
                finally
                {
                    slim.Release();
                }

            });
        }
    }
}
