using linker.client.config;
using linker.config;
using linker.plugins.tuntap.messenger;
using linker.libs;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.tuntap.config;
using linker.tun;
using linker.plugins.tuntap.lease;

namespace linker.plugins.tuntap.client
{
    public sealed class TuntapConfigTransfer
    {
        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly TuntapProxy tuntapProxy;
        private readonly RunningConfig runningConfig;
        private readonly TuntapTransfer tuntapTransfer;
        private readonly LeaseClientTreansfer leaseClientTreansfer;

        private LinkerTunDeviceRouteItem[] routeItems = new LinkerTunDeviceRouteItem[0];

        public VersionManager Version { get; } = new VersionManager();
        private readonly ConcurrentDictionary<string, TuntapInfo> tuntapInfos = new ConcurrentDictionary<string, TuntapInfo>();
        public ConcurrentDictionary<string, TuntapInfo> Infos => tuntapInfos;

        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        public TuntapConfigTransfer(IMessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config, TuntapProxy tuntapProxy, RunningConfig runningConfig, TuntapTransfer tuntapTransfer, LeaseClientTreansfer leaseClientTreansfer)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
            this.runningConfig = runningConfig;
            this.tuntapTransfer = tuntapTransfer;
            this.leaseClientTreansfer = leaseClientTreansfer;


            clientSignInState.NetworkEnabledHandle += (times) => RefreshIP();

            tuntapTransfer.OnSetupBefore += () => { NotifyConfig(); };
            tuntapTransfer.OnSetupAfter += () => { NotifyConfig(); };
            tuntapTransfer.OnSetupSuccess += () => { NotifyConfig(); runningConfig.Data.Tuntap.Running = true; runningConfig.Data.Update(); };

            tuntapTransfer.OnShutdownBefore += () => { NotifyConfig(); };
            tuntapTransfer.OnShutdownAfter += () => { NotifyConfig(); };
            tuntapTransfer.OnShutdownSuccess += () => { NotifyConfig(); DeleteForward(); DelRoute(); runningConfig.Data.Tuntap.Running = false; runningConfig.Data.Update(); };


            InitConfig();
        }
        private void InitConfig()
        {
            if (runningConfig.Data.Tuntap.Lans.Count == 0 && runningConfig.Data.Tuntap.LanIPs.Length > 0)
            {
                runningConfig.Data.Tuntap.Lans = runningConfig.Data.Tuntap.LanIPs.Select((a, b) => new TuntapLanInfo { IP = a, PrefixLength = (byte)runningConfig.Data.Tuntap.Masks[b] }).ToList();
            }
        }

        /// <summary>
        /// 刷新IP
        /// </summary>
        public void RefreshIP()
        {
            TimerHelper.Async(async () =>
            {
                IPAddress oldIP = runningConfig.Data.Tuntap.IP;
                byte prefixLength = runningConfig.Data.Tuntap.PrefixLength;

                await LeaseIP();
                while (tuntapTransfer.Status == TuntapStatus.Operating)
                {
                    await Task.Delay(1000);
                }

                bool run = ((oldIP.Equals(runningConfig.Data.Tuntap.IP) == false || prefixLength != runningConfig.Data.Tuntap.PrefixLength) && runningConfig.Data.Tuntap.Running)
                || (runningConfig.Data.Tuntap.Running && tuntapTransfer.Status != TuntapStatus.Running);

                if (run)
                {
                    tuntapTransfer.Shutdown();
                    tuntapTransfer.Setup(runningConfig.Data.Tuntap.IP, runningConfig.Data.Tuntap.PrefixLength);
                }
                NotifyConfig();
            });

        }
        /// <summary>
        /// 重启网卡
        /// </summary>
        /// <returns></returns>
        public async Task RetstartDevice()
        {
            tuntapTransfer.Shutdown();
            await LeaseIP();
            tuntapTransfer.Setup(runningConfig.Data.Tuntap.IP, runningConfig.Data.Tuntap.PrefixLength);
        }
        /// <summary>
        /// 关闭网卡
        /// </summary>
        public void StopDevice()
        {
            tuntapTransfer.Shutdown();
        }
        /// <summary>
        /// 租赁IP
        /// </summary>
        /// <returns></returns>
        private async Task LeaseIP()
        {
            if (runningConfig.Data.Tuntap.Group2IP.TryGetValue(config.Data.Client.Group.Id, out TuntapGroup2IPInfo tuntapGroup2IPInfo))
            {
                if (tuntapGroup2IPInfo.IP.Equals(runningConfig.Data.Tuntap.IP) == false || tuntapGroup2IPInfo.PrefixLength != runningConfig.Data.Tuntap.PrefixLength)
                {
                    runningConfig.Data.Tuntap.IP = tuntapGroup2IPInfo.IP;
                    runningConfig.Data.Tuntap.PrefixLength = tuntapGroup2IPInfo.PrefixLength;
                }
            }
            if (runningConfig.Data.Tuntap.Running || runningConfig.Data.Tuntap.IP.Equals(IPAddress.Any) == false)
            {
                LeaseInfo leaseInfo = await leaseClientTreansfer.LeaseIp(runningConfig.Data.Tuntap.IP, runningConfig.Data.Tuntap.PrefixLength);
                runningConfig.Data.Tuntap.IP = leaseInfo.IP;
                runningConfig.Data.Tuntap.PrefixLength = leaseInfo.PrefixLength;
                runningConfig.Data.Update();
            }
            tuntapGroup2IPInfo = new TuntapGroup2IPInfo { IP = runningConfig.Data.Tuntap.IP, PrefixLength = runningConfig.Data.Tuntap.PrefixLength };
            runningConfig.Data.Tuntap.Group2IP.AddOrUpdate(config.Data.Client.Group.Id, tuntapGroup2IPInfo, (a, b) => tuntapGroup2IPInfo);
        }

        /// <summary>
        /// 刷新信息，把自己的网卡配置发给别人，顺便把别人的网卡信息带回来
        /// </summary>
        public void RefreshConfig()
        {
            NotifyConfig();
        }
        /// <summary>
        /// 更新本机网卡信息
        /// </summary>
        /// <param name="info"></param>
        public void UpdateConfig(TuntapInfo info)
        {
            TimerHelper.Async(() =>
            {
                DeleteForward();

                bool needReboot = info.IP.Equals(runningConfig.Data.Tuntap.IP) == false || info.PrefixLength != runningConfig.Data.Tuntap.PrefixLength;

                runningConfig.Data.Tuntap.IP = info.IP;
                runningConfig.Data.Tuntap.Lans = info.Lans;
                runningConfig.Data.Tuntap.PrefixLength = info.PrefixLength;
                runningConfig.Data.Tuntap.Switch = info.Switch;
                runningConfig.Data.Tuntap.Forwards = info.Forwards;

                TuntapGroup2IPInfo tuntapGroup2IPInfo = new TuntapGroup2IPInfo { IP = info.IP, PrefixLength = info.PrefixLength };
                runningConfig.Data.Tuntap.Group2IP.AddOrUpdate(config.Data.Client.Group.Id, tuntapGroup2IPInfo, (a, b) => tuntapGroup2IPInfo);

                runningConfig.Data.Update();
                if (tuntapTransfer.Status == TuntapStatus.Running && needReboot)
                {
                    tuntapTransfer.Shutdown();
                    tuntapTransfer.Setup(runningConfig.Data.Tuntap.IP, runningConfig.Data.Tuntap.PrefixLength);
                }
                else
                {
                    AddForward();
                    NotifyConfig();
                }
            });
        }
        /// <summary>
        /// 收到别的客户端的网卡信息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public TuntapInfo OnConfig(TuntapInfo info)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Debug($"tuntap got {info.IP}");
            }

            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();
                try
                {
                    DelRoute();
                    tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
                    Version.Add();
                    AddRoute();
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

            return GetLocalInfo();
        }
        /// <summary>
        /// 信息有变化，刷新信息，把自己的网卡配置发给别人，顺便把别人的网卡信息带回来
        /// </summary>
        private void NotifyConfig()
        {
            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();

                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        List<TuntapInfo> list = await GetRemoteInfo().ConfigureAwait(false);

                        if (list != null)
                        {
                            DelRoute();
                            foreach (var item in list)
                            {
                                tuntapInfos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                                item.LastTicks.Update();
                            }
                            var removes = tuntapInfos.Keys.Except(list.Select(c => c.MachineId)).ToList();
                            foreach (var item in removes)
                            {
                                if (tuntapInfos.TryGetValue(item, out TuntapInfo tuntapInfo))
                                {
                                    tuntapInfo.Status = TuntapStatus.Normal;
                                    tuntapInfo.LastTicks.Clear();
                                }
                            }
                            Version.Add();
                            AddRoute();
                            break;
                        }
                        await Task.Delay(1000);
                    }
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
        /// <summary>
        /// 获取自己的网卡信息
        /// </summary>
        /// <returns></returns>
        private TuntapInfo GetLocalInfo()
        {
            TuntapInfo info = new TuntapInfo
            {
                IP = runningConfig.Data.Tuntap.IP,
                Lans = runningConfig.Data.Tuntap.Lans.Where(c => c.Disabled == false && c.IP.Equals(IPAddress.Any) == false).Select(c => { c.Exists = false;return c; }).ToList(),
                PrefixLength = runningConfig.Data.Tuntap.PrefixLength,
                MachineId = config.Data.Client.Id,
                Status = tuntapTransfer.Status,
                SetupError = tuntapTransfer.SetupError,
                NatError = tuntapTransfer.NatError,
                SystemInfo = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")) == false ? "Docker" : "")}",

                Forwards = runningConfig.Data.Tuntap.Forwards,
                Switch = runningConfig.Data.Tuntap.Switch
            };
            return info;
        }
        /// <summary>
        /// 获取别人的网卡信息
        /// </summary>
        /// <returns></returns>
        private async Task<List<TuntapInfo>> GetRemoteInfo()
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
            {
                LoggerHelper.Instance.Debug($"tuntap sync");
            }

            TuntapInfo info = GetLocalInfo();
            tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.ConfigForward,
                Payload = MemoryPackSerializer.Serialize(info),
                Timeout = 3000
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return null;
            }

            List<TuntapInfo> infos = MemoryPackSerializer.Deserialize<List<TuntapInfo>>(resp.Data.Span);
            infos.Add(info);
            return infos;
        }

        // <summary>
        /// 添加端口转发
        /// </summary>
        private void AddForward()
        {
            tuntapTransfer.AddForward(ParseForwardItems());
        }
        /// <summary>
        /// 删除端口转发
        /// </summary>
        private void DeleteForward()
        {
            tuntapTransfer.RemoveForward(ParseForwardItems());
        }
        private List<LinkerTunDeviceForwardItem> ParseForwardItems()
        {
            return runningConfig.Data.Tuntap.Forwards.Select(c => new LinkerTunDeviceForwardItem { ListenAddr = c.ListenAddr, ListenPort = c.ListenPort, ConnectAddr = c.ConnectAddr, ConnectPort = c.ConnectPort }).ToList();
        }

        /// <summary>
        /// 删除路由
        /// </summary>
        private void DelRoute()
        {
            if (routeItems != null)
                tuntapTransfer.DelRoute(routeItems);
        }
        /// <summary>
        /// 添加路由
        /// </summary>
        private void AddRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapInfos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            routeItems = ipsList.SelectMany(c => c.IPS).Select(c => new LinkerTunDeviceRouteItem { Address = c.OriginIPAddress, PrefixLength = c.PrefixLength }).ToArray();

            tuntapTransfer.AddRoute(routeItems, runningConfig.Data.Tuntap.IP);

            tuntapProxy.SetIPs(ips);
            foreach (var item in tuntapInfos.Values)
            {
                tuntapProxy.SetIP(item.MachineId, NetworkHelper.IP2Value(item.IP));
            }
            Version.Add();
        }

        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            //排除的IP，
            uint[] excludeIps =//本机局域网IP
                config.Data.Client.Tunnel.LocalIPs.Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                //路由上的IP
                .Concat(config.Data.Client.Tunnel.RouteIPs)
                //网卡IP  服务器IP
                .Concat(new IPAddress[] { runningConfig.Data.Tuntap.IP, clientSignInState.Connection.Address.Address })
                //网卡配置的局域网IP
                .Concat(runningConfig.Data.Tuntap.Lans.Select(c => c.IP))
                .Select(NetworkHelper.IP2Value)
                .ToArray();

            HashSet<uint> hashSet = new HashSet<uint>();

            return infos
                .Where(c => c.MachineId != config.Data.Client.Id)
                .OrderByDescending(c => c.Status)
                .OrderByDescending(c => c.LastTicks.Value)

                .Select(c =>
                {
                    Console.WriteLine($"{c.MachineId}->{config.Data.Client.Id}");
                    foreach (var lan in c.Lans)
                    {
                        uint ipInt = NetworkHelper.IP2Value(lan.IP);
                        uint maskValue = NetworkHelper.PrefixLength2Value(lan.PrefixLength);
                        lan.Exists = excludeIps.Count(d => (d & maskValue) == (ipInt & maskValue)) > 0 || hashSet.Contains(ipInt & maskValue);
                        hashSet.Add(ipInt & maskValue);
                    }

                    return new TuntapVeaLanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(c.Lans.Where(c => c.Disabled == false && c.Exists == false).ToList(), c.MachineId)
                        .Where(c => excludeIps.Select(d => d & c.MaskValue).Contains(c.NetWork) == false).ToList(),
                    };
                }).ToList();
        }
        private List<TuntapVeaLanIPAddress> ParseIPs(List<TuntapLanInfo> lans, string machineid)
        {
            return lans.Where(c => c.IP.Equals(IPAddress.Any) == false && c != null).Select((c, index) =>
            {
                return ParseIPAddress(c.IP, c.PrefixLength, machineid);

            }).ToList();
        }
        private TuntapVeaLanIPAddress ParseIPAddress(IPAddress ip, byte prefixLength, string machineid)
        {
            uint ipInt = NetworkHelper.IP2Value(ip);
            //掩码十进制
            uint maskValue = NetworkHelper.PrefixLength2Value(prefixLength);
            return new TuntapVeaLanIPAddress
            {
                IPAddress = ipInt,
                PrefixLength = prefixLength,
                MaskValue = maskValue,
                NetWork = ipInt & maskValue,
                Broadcast = ipInt | ~maskValue,
                OriginIPAddress = ip,
                MachineId = machineid
            };
        }

    }
}
