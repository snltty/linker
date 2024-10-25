using linker.client.config;
using linker.config;
using linker.libs;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.socks5.config;
using linker.plugins.decenter;

namespace linker.plugins.socks5
{
    public sealed class Socks5ConfigTransfer : IDecenter
    {
        public string Name => "socks5";
        public VersionManager DataVersion { get; } = new VersionManager();


        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;
        private readonly TunnelProxy tunnelProxy;

        public VersionManager Version { get; } = new VersionManager();
        private readonly ConcurrentDictionary<string, Socks5Info> socks5Infos = new ConcurrentDictionary<string, Socks5Info>();
        public ConcurrentDictionary<string, Socks5Info> Infos => socks5Infos;



        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        public Socks5ConfigTransfer(IMessengerSender messengerSender, ClientSignInState clientSignInState, FileConfig config, RunningConfig runningConfig, TunnelProxy tunnelProxy)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.config = config;
            this.runningConfig = runningConfig;
            this.tunnelProxy = tunnelProxy;

            clientSignInState.NetworkEnabledHandle += (times) => DataVersion.Add();
        }
        public Memory<byte> GetData()
        {
            Socks5Info info = new Socks5Info
            {
                Lans = runningConfig.Data.Socks5.Lans.Select(c => { c.Exists = false; return c; }).ToList(),
                MachineId = config.Data.Client.Id,
                Status = tunnelProxy.Running ? Socks5Status.Running : Socks5Status.Normal,
                Port = runningConfig.Data.Socks5.Port,
                SetupError = tunnelProxy.Error
            };
            socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
            Version.Add();
            return MemoryPackSerializer.Serialize(info);
        }
        public void SetData(Memory<byte> data)
        {
            Socks5Info info = MemoryPackSerializer.Deserialize<Socks5Info>(data.Span);
            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();
                try
                {
                    socks5Infos.AddOrUpdate(info.MachineId, info, (a, b) => info);
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
        }
        public void SetData(List<ReadOnlyMemory<byte>> data)
        {
            List<Socks5Info> list = data.Select(c => MemoryPackSerializer.Deserialize<Socks5Info>(c.Span)).ToList();
            TimerHelper.Async(async () =>
            {
                await slim.WaitAsync();

                try
                {
                    foreach (var item in list)
                    {
                        socks5Infos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                        item.LastTicks.Update();
                    }
                    var removes = socks5Infos.Keys.Except(list.Select(c => c.MachineId)).ToList();
                    foreach (var item in removes)
                    {
                        if (socks5Infos.TryGetValue(item, out Socks5Info socks5Info))
                        {
                            socks5Info.Status = Socks5Status.Normal;
                            socks5Info.LastTicks.Clear();
                        }
                    }
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
                finally
                {
                    slim.Release();
                }

            });
        }


        /// <summary>
        /// 重启
        /// </summary>
        /// <returns></returns>
        public void Retstart()
        {
            tunnelProxy.Start(runningConfig.Data.Socks5.Port);
        }
        /// <summary>
        /// 网卡
        /// </summary>
        public void Stop()
        {
            tunnelProxy.Stop();
        }

        /// <summary>
        /// 刷新信息，把自己的配置发给别人，顺便把别人的信息带回来
        /// </summary>
        public void RefreshConfig()
        {
            DataVersion.Add();
        }
        /// <summary>
        /// 更新本机信息
        /// </summary>
        /// <param name="info"></param>
        public void UpdateConfig(Socks5Info info)
        {
            TimerHelper.Async(() =>
            {
                int port = runningConfig.Data.Socks5.Port;

                runningConfig.Data.Socks5.Port = info.Port;
                runningConfig.Data.Socks5.Lans = info.Lans;
                runningConfig.Data.Update();

                bool needReboot = (port != runningConfig.Data.Socks5.Port && runningConfig.Data.Socks5.Running)
                || (runningConfig.Data.Socks5.Running && tunnelProxy.Running == false);

                if (needReboot)
                {
                    Retstart();
                }
                GetData();
                DataVersion.Add();
            });
        }

        /// <summary>
        /// 添加路由
        /// </summary>
        private void AddRoute()
        {
            List<Socks5LanIPAddressList> ipsList = ParseIPs(socks5Infos.Values.ToList());
            Socks5LanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();

            tunnelProxy.SetIPs(ips);
            Version.Add();
        }

        private List<Socks5LanIPAddressList> ParseIPs(List<Socks5Info> infos)
        {
            //排除的IP，
            uint[] excludeIps =//本机局域网IP
                config.Data.Client.Tunnel.LocalIPs.Where(c => c.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                //路由上的IP
                .Concat(config.Data.Client.Tunnel.RouteIPs)
                //网卡IP  服务器IP
                .Concat(new IPAddress[] { runningConfig.Data.Tuntap.IP, clientSignInState.Connection.Address.Address })
                //网卡配置的局域网IP
                .Concat(runningConfig.Data.Socks5.Lans.Select(c => c.IP))
                .Select(NetworkHelper.IP2Value)
                .ToArray();

            HashSet<uint> hashSet = new HashSet<uint>();

            return infos
                .Where(c => c.MachineId != config.Data.Client.Id)
                .OrderByDescending(c => c.Status)
                .OrderByDescending(c => c.LastTicks.Value)

                .Select(c =>
                {
                    var lans = c.Lans.Where(c => c.Disabled == false && c.IP.Equals(IPAddress.Any) == false);
                    foreach (var lan in lans)
                    {
                        uint ipInt = NetworkHelper.IP2Value(lan.IP);
                        uint maskValue = NetworkHelper.PrefixLength2Value(lan.PrefixLength);
                        lan.Exists = excludeIps.Count(d => (d & maskValue) == (ipInt & maskValue)) > 0 || hashSet.Contains(ipInt & maskValue);
                        hashSet.Add(ipInt & maskValue);
                    }

                    return new Socks5LanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(lans.Where(c => c.Disabled == false && c.Exists == false).ToList(), c.MachineId)
                        .Where(c => excludeIps.Select(d => d & c.MaskValue).Contains(c.NetWork) == false).ToList(),
                    };
                }).ToList();
        }
        private List<Socks5LanIPAddress> ParseIPs(List<Socks5LanInfo> lans, string machineid)
        {
            return lans.Where(c => c.IP.Equals(IPAddress.Any) == false && c != null).Select((c, index) =>
            {
                return ParseIPAddress(c.IP, c.PrefixLength, machineid);

            }).ToList();
        }
        private Socks5LanIPAddress ParseIPAddress(IPAddress ip, byte prefixLength, string machineid)
        {
            uint ipInt = NetworkHelper.IP2Value(ip);
            //掩码十进制
            uint maskValue = NetworkHelper.PrefixLength2Value(prefixLength);
            return new Socks5LanIPAddress
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
