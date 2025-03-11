using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.tunnel;
using System.Net;
using System.Net.Quic;
using System.Text.Json.Nodes;

namespace linker.messenger.tunnel
{
    public sealed class TunnelNetworkTransfer
    {
        private readonly ISignInClientStore signInClientStore;
        public TunnelNetworkInfo Info { get; private set; } = new TunnelNetworkInfo();

        public TunnelNetworkTransfer(ISignInClientStore signInClientStore, SignInClientState signInClientState, TunnelTransfer tunnelTransfer)
        {
            this.signInClientStore = signInClientStore;

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
                string str = await httpClient.GetStringAsync($"http://ip-api.com/json").WaitAsync(TimeSpan.FromMilliseconds(3000));

                if (string.IsNullOrWhiteSpace(str) == false)
                {
                    TunnelNetInfo net = str.DeJson<TunnelNetInfo>();
                    Info.Net.Isp = net.Isp;
                    Info.Net.As = net.As;
                    Info.Net.Org = net.Org;
                    Info.Net.Region = net.Region;
                    Info.Net.RegionName = net.RegionName;
                    Info.Net.Country = net.Country;
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
                string str = await httpClient.GetStringAsync($"https://api.myip.la/en?json").WaitAsync(TimeSpan.FromMilliseconds(5000));

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
                await Task.WhenAll(GetIsp(), GetPosition());
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

        public sealed class TunnelNetworkInfo
        {
            /// <summary>
            /// 网关层级
            /// </summary>
            public int RouteLevel { get; set; }
            /// <summary>
            /// 本地IP
            /// </summary>
            public IPAddress[] LocalIPs { get; set; } = Array.Empty<IPAddress>();
            /// <summary>
            /// 路由上的IP
            /// </summary>
            public IPAddress[] RouteIPs { get; set; } = Array.Empty<IPAddress>();

            public TunnelNetInfo Net { get; set; } = new TunnelNetInfo();


        }
    }
}
