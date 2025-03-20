using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.tunnel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Quic;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace linker.messenger.tunnel
{
    public sealed class TunnelNetworkTransfer
    {
        private readonly ISignInClientStore signInClientStore;
        private readonly SignInClientState signInClientState;
        public TunnelPublicNetworkInfo Info { get; private set; } = new TunnelPublicNetworkInfo();

        public TunnelNetworkTransfer(ISignInClientStore signInClientStore, SignInClientState signInClientState, TunnelTransfer tunnelTransfer)
        {
            this.signInClientStore = signInClientStore;
            this.signInClientState = signInClientState;

            signInClientState.OnSignInBrfore += GetNet;
            signInClientState.OnSignInSuccessBefore += () =>
            {
                RefreshRouteLevel();
                tunnelTransfer.Refresh();
            };

            TestQuic();

            RefreshRouteLevel();
            tunnelTransfer.Refresh();
        }
        /// <summary>
        /// 刷新网关等级数据
        /// </summary>
        private void RefreshRouteLevel()
        {
            LoggerHelper.Instance.Info($"tunnel route level getting.");
            Info.RouteLevel = NetworkHelper.GetRouteLevel(signInClientStore.Server.Host, out List<IPAddress> ips);
            LoggerHelper.Instance.Warning($"route ips:{string.Join(",", ips.Select(c => c.ToString()))}");
            Info.RouteIPs = ips.ToArray();
            var ipv6 = NetworkHelper.GetIPV6();
            LoggerHelper.Instance.Warning($"tunnel local ip6 :{string.Join(",", ipv6.Select(c => c.ToString()))}");
            var ipv4 = NetworkHelper.GetIPV4();
            LoggerHelper.Instance.Warning($"tunnel local ip4 :{string.Join(",", ipv4.Select(c => c.ToString()))}");
            Info.LocalIPs = ipv6.Concat(ipv4).ToArray();
            LoggerHelper.Instance.Warning($"tunnel route level:{Info.RouteLevel}");
        }

        private async Task GetIsp()
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                string str = await httpClient.GetStringAsync($"http://ip-api.com/json").WaitAsync(TimeSpan.FromMilliseconds(3000)).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(str) == false)
                {
                    TunnelNetInfo net = str.DeJson<TunnelNetInfo>();
                    Info.Net.Isp = net.Isp;
                    Info.Net.CountryCode = net.CountryCode;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Warning(ex);
            }
        }
        private async Task GetPosition()
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                string str = await httpClient.GetStringAsync($"https://api.myip.la/en?json").WaitAsync(TimeSpan.FromMilliseconds(5000)).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(str) == false)
                {
                    JsonNode json = JsonObject.Parse(str);
                    Info.Net.City = json["location"]["city"].ToString();
                    Info.Net.Lat = double.Parse(json["location"]["latitude"].ToString());
                    Info.Net.Lon = double.Parse(json["location"]["longitude"].ToString());
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Warning(ex);
            }
        }
        private async Task GetNet()
        {
            if (string.IsNullOrWhiteSpace(Info.Net.City))
            {
                await Task.WhenAll(GetIsp(), GetPosition()).ConfigureAwait(false);
            }
        }

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
                                Environment.Exit(1);
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


        public TunnelLocalNetworkInfo GetLocalNetwork()
        {
            return new TunnelLocalNetworkInfo
            {
                MachineId = signInClientState.Connection?.Id ?? string.Empty,
                HostName = Dns.GetHostName(),
                Lans = GetInterfaces(),
                Routes = Info.RouteIPs,
            };
        }
        private static byte[] ipv6LocalBytes = new byte[] { 254, 128, 0, 0, 0, 0, 0, 0 };
        private TunnelInterfaceInfo[] GetInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Select(c => new TunnelInterfaceInfo
            {
                Name = c.Name,
                Desc = c.Description,
                Mac = Regex.Replace(c.GetPhysicalAddress().ToString(), @"(.{2})", $"$1-").Trim('-'),
                Ips = c.GetIPProperties().UnicastAddresses.Select(c => c.Address).Where(c => c.AddressFamily == AddressFamily.InterNetwork || (c.AddressFamily == AddressFamily.InterNetworkV6 && c.GetAddressBytes().AsSpan(0, 8).SequenceEqual(ipv6LocalBytes) == false)).ToArray()
            }).Where(c => c.Ips.Length > 0 && c.Ips.Any(d => d.Equals(IPAddress.Loopback)) == false).ToArray();
        }


    }
}
