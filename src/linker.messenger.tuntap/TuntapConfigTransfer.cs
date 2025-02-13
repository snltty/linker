
using linker.libs;
using System.Net;
using linker.messenger.signin;
using linker.messenger.tuntap.lease;

namespace linker.messenger.tuntap
{
    public sealed class TuntapConfigTransfer
    {
        public TuntapConfigInfo Info => tuntapStore.Info;
        public IPAddress IP => Info.IP;
        public bool Running => Info.Running;
        public TuntapSwitch Switch => Info.Switch;

        private string name = string.Empty;
        public string Name => string.IsNullOrWhiteSpace(Info.Name) ? (string.IsNullOrWhiteSpace(name) ? "linker" : name) : Info.Name;

        public VersionManager Version { get; } = new VersionManager();

        private readonly ITuntapClientStore tuntapStore;
        private readonly LeaseClientTreansfer leaseClientTreansfer;
        private readonly ISignInClientStore signInClientStore;

        /// <summary>
        /// 有操作
        /// </summary>
        public Action OnUpdate { get; set; } = () => { };

        public TuntapConfigTransfer(ITuntapClientStore tuntapStore, LeaseClientTreansfer leaseClientTreansfer, ISignInClientStore signInClientStore)
        {
            this.tuntapStore = tuntapStore;
            this.leaseClientTreansfer = leaseClientTreansfer;
            this.signInClientStore = signInClientStore;

        }

        /// <summary>
        /// 保存启动状态，方便下次启动时自动启动网卡
        /// </summary>
        /// <param name="running"></param>
        public void SetRunning(bool running)
        {
            Info.Running = running;
            tuntapStore.Confirm();
            OnUpdate();
        }
        /// <summary>
        /// 更新本机网卡信息，会触发事件
        /// </summary>
        /// <param name="info"></param>
        public void Update(TuntapInfo info)
        {
            TimerHelper.Async(async () =>
            {
                IPAddress ip = Info.IP;
                byte prefixLength = Info.PrefixLength;
                string name = Info.Name;

                Info.IP = info.IP ?? IPAddress.Any;
                Info.Lans = info.Lans;
                Info.PrefixLength = info.PrefixLength;
                Info.Name = info.Name;
                Info.Switch = info.Switch;
                Info.Forwards = info.Forwards;
                tuntapStore.Confirm();

                await LeaseIP();
                SetGroupIP();

                if (ip.Equals(Info.IP) == false || prefixLength != Info.PrefixLength || string.Equals(name, Info.Name) == false)
                {
                    Version.Add();
                }

                OnUpdate();
            });
        }
        /// <summary>
        /// 刷新IP
        /// </summary>
        public void RefreshIP()
        {
            _ = RefreshIPASync();
        }
        /// <summary>
        /// 刷新IP
        /// </summary>
        /// <returns></returns>
        public async Task RefreshIPASync()
        {
            IPAddress oldIP = Info.IP;
            byte prefixLength = Info.PrefixLength;

            LoadGroupIP();
            await LeaseIP();
            SetGroupIP();

            if ((oldIP.Equals(Info.IP) == false || prefixLength != Info.PrefixLength) && Info.Running)
            {
                Version.Add();
            }

            OnUpdate();
        }
        private async Task LeaseIP()
        {
            LeaseInfo leaseInfo = await leaseClientTreansfer.LeaseIp(Info.IP, Info.PrefixLength);
            Info.IP = leaseInfo.IP;
            Info.PrefixLength = leaseInfo.PrefixLength;
            name = leaseInfo.Name;
            tuntapStore.Confirm();
        }

        private void LoadGroupIP()
        {
            if (Info.Group2IP.TryGetValue(signInClientStore.Group.Id, out TuntapGroup2IPInfo tuntapGroup2IPInfo))
            {
                if (tuntapGroup2IPInfo.IP.Equals(Info.IP) == false || tuntapGroup2IPInfo.PrefixLength != Info.PrefixLength)
                {
                    Info.IP = tuntapGroup2IPInfo.IP;
                    Info.PrefixLength = tuntapGroup2IPInfo.PrefixLength;
                }
            }
        }
        private void SetGroupIP()
        {
            TuntapGroup2IPInfo tuntapGroup2IPInfo = new TuntapGroup2IPInfo { IP = Info.IP, PrefixLength = Info.PrefixLength };
            Info.Group2IP.AddOrUpdate(signInClientStore.Group.Id, tuntapGroup2IPInfo, (a, b) => tuntapGroup2IPInfo);
        }

    }
}
