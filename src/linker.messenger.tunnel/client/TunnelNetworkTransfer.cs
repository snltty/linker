using linker.libs;
using linker.libs.extends;
using linker.messenger.decenter;
using linker.messenger.signin;
using linker.messenger.tunnel.stun.clients;
using linker.messenger.tunnel.stun.enums;
using linker.tunnel;
using linker.upnp;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Quic;
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


        public TunnelNetworkTransfer(ISignInClientStore signInClientStore, SignInClientState signInClientState, ITunnelClientStore tunnelClientStore, IMessengerSender messengerSender, ISerializer serializer, TunnelTransfer tunnelTransfer, CounterDecenter counterDecenter)
        {
            this.signInClientStore = signInClientStore;
            this.signInClientState = signInClientState;
            this.tunnelClientStore = tunnelClientStore;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.tunnelTransfer = tunnelTransfer;

            signInClientState.OnSignInSuccessBefore += async () => { await GetRouteLevel().ConfigureAwait(false); tunnelTransfer.Refresh(); };

            Refresh();
            TestQuic();

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
            return PortMappingUtility.Get();
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
            _ = GetNet();
            _ = GetRouteLevel();
            tunnelTransfer.Refresh();
        }
        private async Task GetRouteLevel()
        {
            await Task.Run(async () =>
            {
                if (operatingManager.StartOperation("get_level") == false)
                {
                    return;
                }
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
                operatingManager.StopOperation("get_level");
            }).ConfigureAwait(false);
        }
        private async Task GetNet()
        {

            if (operatingManager.StartOperation("get_net") == false)
            {
                return;
            }

            await Task.Run(async () =>
            {

                try
                {
                    bool isp = false, city = false, nat = false;
                    while (true)
                    {
                        if (isp == false) isp = await GetIsp().ConfigureAwait(false);
                        if (city == false) city = await GetPosition().ConfigureAwait(false);
                        if (nat == false) nat = await GetNat().ConfigureAwait(false);
                        if (isp && city && nat)
                        {
                            break;
                        }
                        await Task.Delay(10000).ConfigureAwait(false);
                    }
                    await tunnelClientStore.SetNetwork(tunnelClientStore.Network);
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = signInClientState.Connection,
                        MessengerId = (ushort)SignInMessengerIds.PushArg,
                        Payload = serializer.Serialize(new SignInPushArgInfo
                        {
                            Key = "tunnelNet",
                            Value = new SignInArgsNetInfo { Lat = tunnelClientStore.Network.Net.Lat, Lon = tunnelClientStore.Network.Net.Lon, City = tunnelClientStore.Network.Net.City }.ToJson()
                        })
                    });
                }
                catch (Exception)
                {
                }

            });
            operatingManager.StopOperation("get_net");
        }
        private async Task<bool> GetNat()
        {
            IPEndPoint server = await NetworkHelper.GetEndPointAsync("stun.hot-chilli.net", 3478);
            await using StunClient5389UDP client = new(server, new IPEndPoint(IPAddress.Any, 0));
            await client.MappingBehaviorTestAsync().ConfigureAwait(false);

            MappingBehavior mapping = client.State?.MappingBehavior ?? MappingBehavior.Unknown;
            await client.FilteringBehaviorTestAsync().ConfigureAwait(false);
            FilteringBehavior filtering = client.State?.FilteringBehavior ?? FilteringBehavior.Unknown;



            bool result = filtering != FilteringBehavior.UnsupportedServer && mapping != MappingBehavior.UnsupportedServer
                && filtering != FilteringBehavior.Unknown && mapping != MappingBehavior.Unknown
                && filtering != FilteringBehavior.None && mapping != MappingBehavior.Fail;
            if (result)
            {
                tunnelClientStore.Network.Net.Nat = $"{mapping}/{filtering}-{GetSuccessRateValue(mapping, filtering)}%";
            }

            return result;
        }
        public static int GetSuccessRateValue(MappingBehavior mapping, FilteringBehavior filtering)
        {
            return (mapping, filtering) switch
            {
                (MappingBehavior.Direct, _) => 100,
                (MappingBehavior.EndpointIndependent, FilteringBehavior.EndpointIndependent) => 98,
                (MappingBehavior.EndpointIndependent, FilteringBehavior.AddressDependent) => 85,
                (MappingBehavior.EndpointIndependent, FilteringBehavior.AddressAndPortDependent) => 80,
                (MappingBehavior.AddressDependent, FilteringBehavior.EndpointIndependent) => 50,
                (MappingBehavior.AddressDependent, FilteringBehavior.AddressDependent) => 40,
                (MappingBehavior.AddressAndPortDependent, FilteringBehavior.EndpointIndependent) => 35,
                (MappingBehavior.AddressAndPortDependent, FilteringBehavior.AddressDependent) => 25,
                (MappingBehavior.AddressAndPortDependent, FilteringBehavior.AddressAndPortDependent) => 3,
                _ => 0
            };
        }
        private async Task<bool> GetIsp()
        {
            using CancellationTokenSource cts = new CancellationTokenSource(3000);
            try
            {
                using HttpClient httpClient = new HttpClient();
                string str = await httpClient.GetStringAsync($"http://ip-api.com/json", cts.Token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(str) == false)
                {
                    TunnelNetInfo net = str.DeJson<TunnelNetInfo>();
                    tunnelClientStore.Network.Net.Isp = net.Isp;
                    tunnelClientStore.Network.Net.CountryCode = net.CountryCode;

                    OnChange?.Invoke();
                    return true;
                }
            }
            catch (Exception)
            {
                cts.Cancel();
            }
            return false;
        }
        private async Task<bool> GetPosition()
        {
            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                using HttpClient httpClient = new HttpClient();
                string str = await httpClient.GetStringAsync($"https://api.myip.la/en?json", cts.Token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(str) == false)
                {
                    JsonNode json = JsonObject.Parse(str);
                    tunnelClientStore.Network.Net.City = json["location"]["city"].ToString();
                    tunnelClientStore.Network.Net.Lat = double.Parse(json["location"]["latitude"].ToString());
                    tunnelClientStore.Network.Net.Lon = double.Parse(json["location"]["longitude"].ToString());
                    OnChange?.Invoke();
                    return true;
                }
            }
            catch (Exception)
            {
                cts.Cancel();
            }
            return false;
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



#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
        private void TestQuic()
        {
            if (OperatingSystem.IsWindows())
            {

                if (QuicListener.IsSupported == false)
                {
                    try
                    {
                        if (File.Exists("msquic-openssl.dll"))
                        {
                            LoggerHelper.Instance.Warning($"copy msquic-openssl.dll -> msquic.dll，please restart linker");
                            File.Move("msquic.dll", "msquic.dll.temp", true);
                            File.Move("msquic-openssl.dll", "msquic.dll", true);

                            if (Environment.UserInteractive == false && OperatingSystem.IsWindows())
                            {
                                Helper.AppExit(1);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                try
                {
                    if (File.Exists("msquic.dll.temp"))
                    {
                        File.Delete("msquic.dll.temp");
                    }
                    if (File.Exists("msquic-openssl.dll"))
                    {
                        File.Delete("msquic-openssl.dll");
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
