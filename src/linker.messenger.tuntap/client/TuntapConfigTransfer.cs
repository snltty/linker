
using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;
using linker.messenger.tuntap.lease;
using System.Net;

namespace linker.messenger.tuntap.client
{
    public sealed class TuntapConfigTransfer
    {
        public TuntapConfigInfo Info => tuntapStore.Info;

        public string Name => string.IsNullOrWhiteSpace(Info.Name) == false
            ? Info.Name
            : string.IsNullOrWhiteSpace(networkInfo.Name) == false
            ? networkInfo.Name
            : "linker";
        public int Mtu => Info.Mtu > 0 ? Info.Mtu : networkInfo.Mtu > 0 ? networkInfo.Mtu : 1420;
        public int MssFix => Info.MssFix > 0 ? Info.MssFix : networkInfo.MssFix > 0 ? networkInfo.MssFix : 0;
        public TuntapVlsmStatus VlsmStatus => Info.VlsmStatus == TuntapVlsmStatus.None ? networkInfo.VlsmStatus : Info.VlsmStatus;
        public byte PrefixLength => networkInfo.IP.Equals(IPAddress.Any) ? Info.PrefixLength : networkInfo.PrefixLength;

        public int SubCount => networkInfo.Subs.Count;

        public LeaseInfo Network => networkInfo;

        private LeaseInfo networkInfo = new LeaseInfo();
        private ulong configVersion = 0;
        public bool Changed
        {
            get
            {
                bool result = Version.Eq(configVersion, out ulong version);
                configVersion = version;
                return result == false;
            }
        }

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
                string oldStr = string.Join(",", [
                    $"{Info.IP}",
                    $"{Info.PrefixLength}",
                    $"{Info.Name}",
                    $"{Info.NetworkName}",
                    $"{Info.Mtu}",
                    $"{Info.MssFix}"
                ]);

                Info.IP = info.IP ?? IPAddress.Any;
                Info.Lans = info.Lans;
                Info.PrefixLength = info.PrefixLength;
                Info.Name = info.Name;
                Info.Switch = info.Switch;
                Info.Forwards = info.Forwards;
                Info.NetworkName = info.NetworkName;
                Info.Mtu = info.Mtu;
                Info.MssFix = info.MssFix;
                Info.VlsmStatus = info.VlsmStatus;

                string newStr = string.Join(",", [
                    $"{Info.IP}",
                    $"{Info.PrefixLength}",
                    $"{Info.Name}",
                    $"{Info.NetworkName}",
                    $"{Info.Mtu}",
                    $"{Info.MssFix}"
                ]);

                tuntapStore.Confirm();

                await LeaseIP().ConfigureAwait(false);
                SetGroupIP();

                if (newStr != oldStr)
                {
                    Version.Increment();
                }

                OnUpdate();
            });
        }

        public void SetID(Guid guid)
        {
            Info.Guid = guid;
            tuntapStore.Confirm();
        }

        /// <summary>
        /// 刷新IP
        /// </summary>
        public void RefreshIP()
        {
            _ = RefreshIPAsync();
        }
        /// <summary>
        /// 刷新IP
        /// </summary>
        /// <returns></returns>
        public async Task RefreshIPAsync()
        {
            IPAddress oldIP = Info.IP;
            byte prefixLength = Info.PrefixLength;

            LoadGroupIP();
            await LeaseIP().ConfigureAwait(false);
            SetGroupIP();

            if ((oldIP.Equals(Info.IP) == false || prefixLength != Info.PrefixLength) && Info.Running)
            {
                Version.Increment();
            }

            OnUpdate();
        }

        /// <summary>
        /// 申请IP
        /// </summary>
        /// <returns></returns>
        private async Task LeaseIP()
        {
            networkInfo = await leaseClientTreansfer.GetNetwork().ConfigureAwait(false);
            var leaseInfo = await leaseClientTreansfer.LeaseIp(Info.IP, Info.PrefixLength, Info.NetworkName, Info.Name, Info.Mtu, Info.MssFix, Info.VlsmStatus).ConfigureAwait(false);
            Info.IP = leaseInfo.IP;
            Info.PrefixLength = leaseInfo.PrefixLength;

            tuntapStore.Confirm();
        }

        /// <summary>
        /// 从分组加载IP配置
        /// </summary>
        private void LoadGroupIP()
        {
            if (Info.Group2IP.TryGetValue(signInClientStore.Group.Id, out TuntapGroup2IPInfo tuntapGroup2IPInfo))
            {
                if (tuntapGroup2IPInfo.IP.Equals(Info.IP) == false || tuntapGroup2IPInfo.PrefixLength != Info.PrefixLength || tuntapGroup2IPInfo.NetworkName != Info.NetworkName)
                {
                    Info.IP = tuntapGroup2IPInfo.IP;
                    Info.PrefixLength = tuntapGroup2IPInfo.PrefixLength;
                    Info.NetworkName = tuntapGroup2IPInfo.NetworkName;
                    Info.Mtu = tuntapGroup2IPInfo.Mtu;
                    Info.MssFix = tuntapGroup2IPInfo.MssFix;
                    Info.Name = tuntapGroup2IPInfo.Name;
                }
            }
        }
        /// <summary>
        /// 把IP配置保存到分组
        /// </summary>
        private void SetGroupIP()
        {
            TuntapGroup2IPInfo tuntapGroup2IPInfo = new TuntapGroup2IPInfo
            {
                IP = Info.IP,
                PrefixLength = Info.PrefixLength,
                NetworkName = Info.NetworkName,
                Mtu = Info.Mtu,
                MssFix = Info.MssFix,
                Name = Info.Name,
            };
            Info.Group2IP[signInClientStore.Group.Id] = tuntapGroup2IPInfo;
        }

    }
}
