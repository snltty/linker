using linker.libs;
using linker.messenger.decenter;
using System.Collections.Concurrent;
using System.Net;
using linker.messenger.signin;

namespace linker.messenger.socks5
{
    public sealed class Socks5Decenter : IDecenter
    {
        public string Name => "socks5";
        public VersionManager PushVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public bool Force => socks5Infos.Count < 2;

        private readonly ConcurrentDictionary<string, Socks5Info> socks5Infos = new ConcurrentDictionary<string, Socks5Info>();
        public ConcurrentDictionary<string, Socks5Info> Infos => socks5Infos;


        public Action OnRefresh { get; set; } = () => { };
        public Action OnChanged { get; set; } = () => { };
        public Action OnClear { get; set; } = () => { };

        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        private readonly ISocks5Store socks5Store;

        public Socks5Decenter(SignInClientState signInClientState, ISignInClientStore signInClientStore, ISerializer serializer, ISocks5Store socks5Store)
        {
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.socks5Store = socks5Store;

            signInClientState.OnSignInSuccess += (times) => Refresh();
        }

        /// <summary>
        /// 刷新信息，把自己的配置发给别人，顺便把别人的信息带回来
        /// </summary>
        public void Refresh()
        {
            PushVersion.Increment();
            OnRefresh();
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(new Socks5Info
            {
                Lans = socks5Store.Lans.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false).Select(c => { c.Exists = false; return c; }).ToList(),
                MachineId = signInClientStore.Id,
                Status = socks5Store.Running ? Socks5Status.Running : Socks5Status.Normal,
                Port = socks5Store.Port,
                SetupError = socks5Store.Error,
                Wan = signInClientState.WanAddress.Address
            });
        }
        public void AddData(Memory<byte> data)
        {
            Socks5Info info = serializer.Deserialize<Socks5Info>(data.Span);
            socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
        }
        public void AddData(List<ReadOnlyMemory<byte>> data)
        {
            List<Socks5Info> list = data.Select(c =>
            {
                try
                {
                    return serializer.Deserialize<Socks5Info>(c.Span);
                }
                catch (Exception)
                {
                }
                return null;

            }).Where(c => c != null).ToList();
            foreach (var item in list)
            {
                socks5Infos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                item.LastTicks.Update();
            }
        }
        public void ClearData()
        {
            socks5Infos.Clear();
            OnClear();
        }
        public void ProcData()
        {
            OnChanged();
        }
    }
}
