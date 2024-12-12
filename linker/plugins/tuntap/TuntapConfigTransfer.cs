using linker.client.config;
using linker.libs;
using System.Net;
using linker.plugins.client;
using linker.plugins.tuntap.config;
using linker.plugins.tuntap.lease;

namespace linker.plugins.tuntap
{
    public sealed class TuntapConfigTransfer
    {
        public TuntapConfigInfo Info => runningConfig.Data.Tuntap;
        public IPAddress IP => Info.IP;
        public bool Running => Info.Running;
        public TuntapSwitch Switch => Info.Switch;

        public string DeviceName => "linker";

        private readonly RunningConfig runningConfig;
        private readonly LeaseClientTreansfer leaseClientTreansfer;
        private readonly ClientConfigTransfer clientConfigTransfer;

        public Action OnUpdate { get; set; } = () => { };
        public Func<Task> OnChanged { get; set; } = async () => { await Task.CompletedTask; };

        public TuntapConfigTransfer(RunningConfig runningConfig, LeaseClientTreansfer leaseClientTreansfer, ClientConfigTransfer clientConfigTransfer)
        {
            this.runningConfig = runningConfig;
            this.leaseClientTreansfer = leaseClientTreansfer;
            this.clientConfigTransfer = clientConfigTransfer;
        }

        /// <summary>
        /// 保存启动状态，方便下次启动时自动启动网卡
        /// </summary>
        /// <param name="running"></param>
        public void SetRunning(bool running)
        {
            Info.Running = running;
            runningConfig.Data.Update();
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
                runningConfig.Data.Update();

                await LeaseIP();
                SetGroupIP();

                if ((ip.Equals(Info.IP) == false || prefixLength != Info.PrefixLength) && Info.Running)
                {
                    await OnChanged();
                }

                OnUpdate();
            });
        }
        /// <summary>
        /// 刷新IP，会触发OnChanged事件
        /// </summary>
        public void RefreshIP()
        {
            TimerHelper.Async(async () =>
            {
                IPAddress oldIP = Info.IP;
                byte prefixLength = Info.PrefixLength;

                await RefreshIPASync();

                if ((oldIP.Equals(Info.IP) == false || prefixLength != Info.PrefixLength) && Info.Running)
                {
                    await OnChanged();
                }
                OnUpdate();
            });
        }
        /// <summary>
        /// 刷新IP，不会触发OnChanged
        /// </summary>
        /// <returns></returns>
        public async Task RefreshIPASync()
        {
            LoadGroupIP();
            await LeaseIP();
            SetGroupIP();
            OnUpdate();
        }
        private async Task LeaseIP()
        {
            LeaseInfo leaseInfo = await leaseClientTreansfer.LeaseIp(Info.IP, Info.PrefixLength);
            Info.IP = leaseInfo.IP;
            Info.PrefixLength = leaseInfo.PrefixLength;
            runningConfig.Data.Update();
        }

        private void LoadGroupIP()
        {
            if (Info.Group2IP.TryGetValue(clientConfigTransfer.Group.Id, out TuntapGroup2IPInfo tuntapGroup2IPInfo))
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
            Info.Group2IP.AddOrUpdate(clientConfigTransfer.Group.Id, tuntapGroup2IPInfo, (a, b) => tuntapGroup2IPInfo);
        }

    }
}
