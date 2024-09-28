using linker.client.config;
using linker.config;
using linker.plugins.tuntap.messenger;
using linker.libs;
using MemoryPack;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.tuntap.config;
using linker.tun;
using linker.tunnel.connection;

namespace linker.plugins.tuntap
{
    public sealed class TuntapTransfer
    {
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly FileConfig config;
        private readonly TuntapProxy tuntapProxy;
        private readonly RunningConfig runningConfig;
        private readonly LinkerTunDeviceAdapter linkerTunDeviceAdapter;

        private string deviceName = "linker";
        private List<IPAddress> routeIps = new List<IPAddress>();

        public VersionManager Version { get; } = new VersionManager();
        private readonly ConcurrentDictionary<string, TuntapInfo> tuntapInfos = new ConcurrentDictionary<string, TuntapInfo>();
        public ConcurrentDictionary<string, TuntapInfo> Infos => tuntapInfos;


        public LinkerTunDeviceRouteItem[] RouteItems { get; private set; } = [];


        private OperatingManager operatingManager = new OperatingManager();
        public TuntapStatus Status => operatingManager.Operating ? TuntapStatus.Operating : (TuntapStatus)(byte)linkerTunDeviceAdapter.Status;

        private readonly SemaphoreSlim slim = new SemaphoreSlim(1);
        public TuntapTransfer(MessengerSender messengerSender, ClientSignInState clientSignInState, LinkerTunDeviceAdapter linkerTunDeviceAdapter, FileConfig config, TuntapProxy tuntapProxy, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.linkerTunDeviceAdapter = linkerTunDeviceAdapter;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
            this.runningConfig = runningConfig;

            linkerTunDeviceAdapter.Initialize(deviceName, tuntapProxy);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => linkerTunDeviceAdapter.Shutdown();
            Console.CancelKeyPress += (s, e) => linkerTunDeviceAdapter.Shutdown();
            clientSignInState.NetworkFirstEnabledHandle += Initialize;
            clientSignInState.NetworkEnabledHandle += (times) => NotifyConfig();

        }
        private void Initialize()
        {
            TimerHelper.Async(() =>
            {
                LoggerHelper.Instance.Debug($"tuntap initialize");
                linkerTunDeviceAdapter.Shutdown();
                linkerTunDeviceAdapter.Clear();
                NetworkHelper.GetRouteLevel(config.Data.Client.ServerInfo.Host, out routeIps);
                NotifyConfig();
                CheckTuntapStatusTask();
                PingTask();
                if (runningConfig.Data.Tuntap.Running)
                {
                    LoggerHelper.Instance.Debug($"tuntap should be run");
                    Setup();
                }
            });
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        public void Setup()
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            TimerHelper.Async(() =>
            {
                SetupBefore();
                try
                {
                    if (runningConfig.Data.Tuntap.IP.Equals(IPAddress.Any))
                    {
                        return;
                    }
                    linkerTunDeviceAdapter.Setup(runningConfig.Data.Tuntap.IP, runningConfig.Data.Tuntap.PrefixLength, 1400);
                    if (string.IsNullOrWhiteSpace(linkerTunDeviceAdapter.Error))
                    {
                        SetupSuccess();
                    }
                    else
                    {
                        LoggerHelper.Instance.Error(linkerTunDeviceAdapter.Error);
                    }
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
                    SetupAfter();
                }
            });
        }
        private void SetupBefore()
        {
            NotifyConfig();
        }
        private void SetupAfter()
        {
            operatingManager.StopOperation();
            NotifyConfig();
        }
        private void SetupSuccess()
        {
            linkerTunDeviceAdapter.SetNat();
            AddForward();
            runningConfig.Data.Tuntap.Running = true;
            runningConfig.Data.Update();
        }

        /// <summary>
        /// 停止网卡
        /// </summary>
        public void Shutdown()
        {
            if (operatingManager.StartOperation() == false)
            {
                return;
            }
            try
            {
                ShutdownBefore();
                linkerTunDeviceAdapter.Shutdown();
                ShutdownSuccess();
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
                ShutdownAfter();
            }
        }
        private void ShutdownBefore()
        {
            NotifyConfig();
        }
        private void ShutdownAfter()
        {
            operatingManager.StopOperation();
            NotifyConfig();
        }
        private void ShutdownSuccess()
        {
            linkerTunDeviceAdapter.RemoveNat();
            DeleteForward();
            runningConfig.Data.Tuntap.Running = false;
            runningConfig.Data.Update();
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
                runningConfig.Data.Tuntap.LanIPs = info.LanIPs;
                runningConfig.Data.Tuntap.Masks = info.Masks;
                runningConfig.Data.Tuntap.PrefixLength = info.PrefixLength;
                runningConfig.Data.Tuntap.Switch = info.Switch;
                runningConfig.Data.Tuntap.Forwards = info.Forwards;
                runningConfig.Data.Update();
                if (Status == TuntapStatus.Running && needReboot)
                {
                    Shutdown();
                    Setup();
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
                LoggerHelper.Instance.Debug($"tuntap got  {info.MachineId}");
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
                LanIPs = runningConfig.Data.Tuntap.LanIPs,
                Masks = runningConfig.Data.Tuntap.Masks,
                PrefixLength = runningConfig.Data.Tuntap.PrefixLength,
                MachineId = config.Data.Client.Id,
                Status = Status,
                Error = linkerTunDeviceAdapter.Error,
                Error1 = linkerTunDeviceAdapter.Error1,
                SystemInfo = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")) == false ? "Docker" : "")}",

                Forwards = runningConfig.Data.Tuntap.Forwards,
                Switch = runningConfig.Data.Tuntap.Switch
            };
            if (runningConfig.Data.Tuntap.Masks.Length != runningConfig.Data.Tuntap.LanIPs.Length)
            {
                runningConfig.Data.Tuntap.Masks = runningConfig.Data.Tuntap.LanIPs.Select(c => 24).ToArray();
            }

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
            linkerTunDeviceAdapter.AddForward(ParseForwardItems());
        }
        /// <summary>
        /// 删除端口转发
        /// </summary>
        private void DeleteForward()
        {
            linkerTunDeviceAdapter.RemoveForward(ParseForwardItems());
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
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapInfos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            var items = ipsList.SelectMany(c => c.IPS).Select(c => new LinkerTunDeviceRouteItem { Address = c.OriginIPAddress, PrefixLength = c.MaskLength }).ToArray();

            linkerTunDeviceAdapter.DelRoute(items);
        }
        /// <summary>
        /// 添加路由
        /// </summary>
        private void AddRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapInfos.Values.Where(c => c.Status == TuntapStatus.Running).ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            var items = ipsList.SelectMany(c => c.IPS).Select(c => new LinkerTunDeviceRouteItem { Address = c.OriginIPAddress, PrefixLength = c.MaskLength }).ToArray();
            RouteItems = items;

            linkerTunDeviceAdapter.AddRoute(items, runningConfig.Data.Tuntap.IP);

            tuntapProxy.SetIPs(ips);
            foreach (var item in tuntapInfos.Values)
            {
                tuntapProxy.SetIP(item.MachineId, BinaryPrimitives.ReadUInt32BigEndian(item.IP.GetAddressBytes()));
            }
            CheckLanIPs();
        }
        /// <summary>
        /// 检查是否有重复的局域网IP
        /// </summary>
        /// <param name="infos"></param>
        private void CheckLanIPs()
        {
            uint[] localIps = NetworkHelper.GetIPV4().Concat(routeIps)
                .Select(c => BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes()))
                .ToArray();

            var ips = tuntapInfos.Values.Where(c => c.MachineId != config.Data.Client.Id).Select(c =>
                {
                    return new TuntapVeaLanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(c.LanIPs, c.Masks, c.MachineId).Where(c => localIps.Select(d => d & c.MaskValue).Contains(c.NetWork)).ToList(),
                    };
                }).ToList();

            foreach (var item in ips)
            {
                if (item.IPS.Count == 0) continue;
                if (tuntapInfos.TryGetValue(item.MachineId, out TuntapInfo info) == false || string.IsNullOrWhiteSpace(info.Error1) == false) continue;
                info.Error1 = $"this machine already has {string.Join(",", item.IPS.Select(c => $"{c.OriginIPAddress}/{c.MaskLength}"))}";
            }
            Version.Add();
        }


        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            uint[] localIps = NetworkHelper.GetIPV4()
                .Concat(new IPAddress[] { runningConfig.Data.Tuntap.IP })
                .Concat(runningConfig.Data.Tuntap.LanIPs.Where(c => c != null))
                .Concat(routeIps)
                .Select(c => BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes()))
                .ToArray();

            return infos
                //自己的ip不要
                .Where(c => c.IP.Equals(runningConfig.Data.Tuntap.IP) == false && c.LastTicks.Greater(0))
                .OrderByDescending(c => c.LastTicks.Value)
                .Select(c =>
                {
                    return new TuntapVeaLanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(c.LanIPs, c.Masks, c.MachineId)
                        //这边的局域网IP也不要，为了防止将本机局域网IP路由到别的地方
                        .Where(c => localIps.Select(d => d & c.MaskValue).Contains(c.NetWork) == false).ToList(),
                    };
                }).ToList();
        }
        private List<TuntapVeaLanIPAddress> ParseIPs(IPAddress[] lanIPs, int[] masks, string machineid)
        {
            if (masks.Length != lanIPs.Length) masks = lanIPs.Select(c => 24).ToArray();
            return lanIPs.Where(c => c.Equals(IPAddress.Any) == false && c != null).Select((c, index) =>
            {
                return ParseIPAddress(c, (byte)masks[index], machineid);

            }).ToList();
        }
        private TuntapVeaLanIPAddress ParseIPAddress(IPAddress ip, byte maskLength, string machineid)
        {
            uint ipInt = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            //掩码十进制
            uint maskValue = NetworkHelper.GetPrefixIP(maskLength);
            return new TuntapVeaLanIPAddress
            {
                IPAddress = ipInt,
                MaskLength = maskLength,
                MaskValue = maskValue,
                NetWork = ipInt & maskValue,
                Broadcast = ipInt | (~maskValue),
                OriginIPAddress = ip,
                MachineId = machineid
            };
        }

        private void CheckTuntapStatusTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (runningConfig.Data.Tuntap.Running && OperatingSystem.IsWindows())
                {
                    await InterfaceCheck().ConfigureAwait(false);
                }
                return true;
            }, 15000);
        }
        private async Task InterfaceCheck()
        {
            if (await InterfaceAvailable() == false && operatingManager.Operating == false)
            {
                LoggerHelper.Instance.Error($"tuntap inerface {deviceName} is down, restarting");
                linkerTunDeviceAdapter.Shutdown();
                await Task.Delay(5000).ConfigureAwait(false);
                if (await InterfaceAvailable() == false && operatingManager.Operating == false)
                {
                    Setup();
                }
            }
        }
        private async Task<bool> InterfaceAvailable()
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == deviceName);
            return networkInterface != null && networkInterface.OperationalStatus == OperationalStatus.Up && await InterfacePing();
        }
        private async Task<bool> InterfacePing()
        {
            try
            {
                using Ping ping = new Ping();
                PingReply pingReply = await ping.SendPingAsync(runningConfig.Data.Tuntap.IP, 500);
                return pingReply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }



        private readonly LastTicksManager lastTicksManager = new LastTicksManager();
        public void SubscribePing()
        {
            lastTicksManager.Update();
        }
        private void PingTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (lastTicksManager.Less(5000))
                {
                    await Ping();
                }
                return true;
            }, 3000);
            TimerHelper.SetInterval(async () =>
            {
                if (lastTicksManager.Greater(15000))
                {
                    await Ping();
                }
                return true;
            }, 30000);
        }
        private async Task Ping()
        {
            if (Status == TuntapStatus.Running && (runningConfig.Data.Tuntap.Switch & TuntapSwitch.ShowDelay) == TuntapSwitch.ShowDelay)
            {
                var items = tuntapInfos.Values.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false && (c.Status & TuntapStatus.Running) == TuntapStatus.Running);
                if ((runningConfig.Data.Tuntap.Switch & TuntapSwitch.AutoConnect) != TuntapSwitch.AutoConnect)
                {
                    var connections = tuntapProxy.GetConnections();
                    items = items.Where(c => (connections.TryGetValue(c.MachineId, out ITunnelConnection connection) && connection.Connected) || c.MachineId == config.Data.Client.Id);
                }

                foreach (var item in items)
                {
                    using Ping ping = new Ping();
                    PingReply pingReply = await ping.SendPingAsync(item.IP, 500);
                    item.Delay = pingReply.Status == IPStatus.Success ? (int)pingReply.RoundtripTime : -1;

                    Version.Add();
                }
            }
        }
    }
}
