
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

        public VersionManager Version { get; } = new VersionManager();

        public string DeviceName => "linker";

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

                Info.IP = info.IP ?? IPAddress.Any;
                Info.Lans = info.Lans;
                Info.PrefixLength = info.PrefixLength;
                Info.Switch = info.Switch;
                Info.Forwards = info.Forwards;
                tuntapStore.Confirm();

                await LeaseIP();
                SetGroupIP();

                if ((ip.Equals(Info.IP) == false || prefixLength != Info.PrefixLength))
                {
                    Version.Add();
                }

                OnUpdate();
            });
        }
        /// <summary>
        /// 刷新IP，会触发OnChanged事件
        /// </summary>
        public void RefreshIP()
        {
            _ = RefreshIPASync();
        }
        /// <summary>
        /// 刷新IP，不会触发OnChanged
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
