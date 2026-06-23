using linker.libs;
using linker.libs.extends;
using linker.messenger.decenter;
using linker.messenger.signin;
using linker.stun;
using linker.tunnel;
using linker.upnp;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace linker.messenger.tunnel.client
{
    public sealed class TunnelNetworkTransfer
    {
        private readonly ISignInClientStore signInClientStore;
        private readonly SignInClientState signInClientState;
        private readonly ITunnelClientStore tunnelClientStore;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly TunnelTransfer tunnelTransfer;

        public Action OnChange { get; set; } = () => { };

        private readonly OperatingMultipleManager operatingManager = new OperatingMultipleManager();

        public TunnelNetworkTransfer(ISignInClientStore signInClientStore, SignInClientState signInClientState,
            ITunnelClientStore tunnelClientStore, IMessengerSender messengerSender, ISerializer serializer,
            TunnelTransfer tunnelTransfer, CounterDecenter counterDecenter)
        {
            this.signInClientStore = signInClientStore;
            this.signInClientState = signInClientState;
            this.tunnelClientStore = tunnelClientStore;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.tunnelTransfer = tunnelTransfer;

            signInClientState.OnSignInSuccessBefore += async () => { RefreshRouteLevel(); tunnelTransfer.Refresh(); };

            Refresh();

            PortMappingUtility.StartDiscovery();
            PortMappingUtility.OnChange += () =>
            {
                counterDecenter.SetValue("upnp-d", PortMappingUtility.DeviceCount);
                counterDecenter.SetValue("upnp-r", PortMappingUtility.MappingCount);
                counterDecenter.SetValue("upnp-l", PortMappingUtility.LocalMappingCount);
                counterDecenter.SetValue("upnp-w", PortMappingUtility.WanCount);
            };
        }

        public List<PortMappingInfo> GetMapping()
        {
            return PortMappingUtility.GetRemote();
        }
        public List<PortMappingInfo> GetMappingLocal()
        {
            return PortMappingUtility.GetLocal();
        }
        public async Task AddMapping(PortMappingInfo mapping)
        {
            await PortMappingUtility.Add(mapping).ConfigureAwait(false);
        }
        public async Task DelMapping(int publicPort, ProtocolType ProtocolType)
        {
            await PortMappingUtility.Delete(publicPort, ProtocolType).ConfigureAwait(false);
        }


        public void Refresh()
        {
            RefreshNet();
            RefreshRouteLevel();
            tunnelTransfer.Refresh();
        }
        private void RefreshRouteLevel()
        {
            operatingManager.StartOperation("get_level", async () =>
            {
                try
                {
                    LoggerHelper.Instance.Info($"tunnel route level getting.");
                    (int level, List<IPAddress> ips) = await NetworkHelper.GetRouteLevel(signInClientStore.Server.Host).ConfigureAwait(false);
                    tunnelClientStore.Network.RouteLevel = level;
                    LoggerHelper.Instance.Warning($"route ips:{string.Join(",", ips.Select(c => c.ToString()))}");
                    tunnelClientStore.Network.RouteIPs = ips.ToArray();
                    var ipv6 = NetworkHelper.GetIPV6();
                    LoggerHelper.Instance.Warning($"tunnel local ip6 :{string.Join(",", ipv6.Select(c => c.ToString()))}");
                    var ipv4 = NetworkHelper.GetIPV4();
                    LoggerHelper.Instance.Warning($"tunnel local ip4 :{string.Join(",", ipv4.Select(c => c.ToString()))}");
                    tunnelClientStore.Network.LocalIPs = ipv6.Concat(ipv4).ToArray();
                    LoggerHelper.Instance.Warning($"tunnel route level:{tunnelClientStore.Network.RouteLevel}");

                    await tunnelClientStore.SetNetwork(tunnelClientStore.Network).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }

            });
        }

        sealed class NetInfo
        {
            public int Flag { get; set; }
            public Func<CancellationToken, Task<int>> Func { get; set; }
        }
        private void RefreshNet()
        {
            operatingManager.StartOperation("get_net", async () =>
            {
                List<NetInfo> netInfos = new List<NetInfo>
                {
                    new NetInfo { Flag = 0, Func = GetIsp },
                    new NetInfo { Flag = 0, Func = GetPosition },
                    new NetInfo { Flag = 0, Func = GetNat }
                };
                while (true)
                {
                    try
                    {
                        using CancellationTokenSource cts = new CancellationTokenSource(30000);
                        int[] results = await Task.WhenAll(netInfos.Where(c => c.Flag == 0).Select(async c =>
                        {
                            int value = await c.Func(cts.Token).ConfigureAwait(false);
                            c.Flag += value;
                            return value;
                        }).ToList()).ConfigureAwait(false);

                        if (results.Any(c => c > 0))
                        {
                            await UploadNet().ConfigureAwait(false);
                        }
                        if (netInfos.All(c => c.Flag > 0))
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            LoggerHelper.Instance.Error(ex);
                    }

                    await Task.Delay(10000).ConfigureAwait(false);
                }
            });
        }
        private async Task UploadNet()
        {
            OnChange?.Invoke();
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SignInMessengerIds.PushArg,
                Payload = serializer.Serialize(new SignInPushArgInfo
                {
                    Key = "tunnelNet",
                    Value = new SignInArgsNetInfo
                    {
                        Lat = tunnelClientStore.Network.Net.Lat,
                        Lon = tunnelClientStore.Network.Net.Lon,
                        City = tunnelClientStore.Network.Net.City
                    }.ToJson()
                })
            });
        }
        private readonly StunClient stun = new StunClient();
        private async Task<int> GetNat(CancellationToken token)
        {
            try
            {
                StunNatBehaviorResult result = await stun.DiscoverNatBehaviorAsync("stunserver2025.stunprotocol.org", 3478, new StunClientOptions
                {
                    AddressFamilyMode = StunAddressFamilyMode.Ipv6Preferred,
                    MaxAttempts = 3
                }, token).ConfigureAwait(false);
                StunNatMappingBehavior mapping = result.MappingBehavior;
                StunNatFilteringBehavior filtering = result.FilteringBehavior;

                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"NAT {result.Status} {mapping}/{filtering}");

                if (result.Status == StunNatBehaviorStatus.Success)
                {
                    tunnelClientStore.Network.Net.Nat = result.P2PSummary;
                    await tunnelClientStore.SetNetwork(tunnelClientStore.Network).ConfigureAwait(false);

                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            return 0;
        }
        private async Task<int> GetIsp(CancellationToken token)
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                string str = await httpClient.GetStringAsync($"http://ip-api.com/json", token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(str) == false)
                {
                    JsonNode json = JsonObject.Parse(str);
                    tunnelClientStore.Network.Net.Isp = json["isp"].ToString();
                    tunnelClientStore.Network.Net.CountryCode = json["countryCode"].ToString();
                    tunnelClientStore.Network.Net.Lat = double.Parse(json["lat"].ToString());
                    tunnelClientStore.Network.Net.Lon = double.Parse(json["lon"].ToString());
                    await tunnelClientStore.SetNetwork(tunnelClientStore.Network);
                    return 1;
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            return 0;
        }
        private async Task<int> GetPosition(CancellationToken token)
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                string str = await httpClient.GetStringAsync($"https://api.myip.la/en?json", token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(str) == false)
                {
                    JsonNode json = JsonObject.Parse(str);
                    tunnelClientStore.Network.Net.City = json["location"]["city"].ToString();
                    tunnelClientStore.Network.Net.Lat = double.Parse(json["location"]["latitude"].ToString());
                    tunnelClientStore.Network.Net.Lon = double.Parse(json["location"]["longitude"].ToString());
                    await tunnelClientStore.SetNetwork(tunnelClientStore.Network);
                    return 1;
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            return 0;
        }

        public TunnelLocalNetworkInfo GetNetwork()
        {
            return new TunnelLocalNetworkInfo
            {
                MachineId = signInClientState.Connection?.Id ?? string.Empty,
                HostName = Dns.GetHostName(),
                Lans = GetInterfaces(),
                Routes = tunnelClientStore.Network.RouteIPs,
            };
        }
        private TunnelInterfaceInfo[] GetInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Select(c => new TunnelInterfaceInfo
            {
                Name = c.Name,
                Desc = c.Description,
                Mac = Regex.Replace(c.GetPhysicalAddress().ToString(), @"(.{2})", $"$1-").Trim('-'),
                Ips = c.GetIPProperties().UnicastAddresses.Select(c => c.Address)
                .Where(c => c.AddressFamily == AddressFamily.InterNetwork || (c.AddressFamily == AddressFamily.InterNetworkV6 && c.GetAddressBytes().AsSpan(0, 8).SequenceEqual(new byte[] { 254, 128, 0, 0, 0, 0, 0, 0 }) == false)).ToArray()
            }).Where(c => c.Ips.Length > 0 && c.Ips.Any(d => d.Equals(IPAddress.Loopback)) == false).ToArray();
        }

    }
}
