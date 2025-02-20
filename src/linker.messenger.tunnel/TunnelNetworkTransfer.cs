using linker.libs;
using linker.libs.extends;
using linker.messenger.signin;
using linker.tunnel;
using System.Net;
using System.Net.Quic;

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
            Info.RouteIPs = ips.ToArray();
            Info.LocalIPs = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
            LoggerHelper.Instance.Warning($"route ips:{string.Join(",", ips.Select(c => c.ToString()))}");
            LoggerHelper.Instance.Warning($"tunnel local ips :{string.Join(",", Info.LocalIPs.Select(c => c.ToString()))}");
            LoggerHelper.Instance.Warning($"tunnel route level:{Info.RouteLevel}");
        }

        private async Task GetNet()
        {
            if (string.IsNullOrWhiteSpace(Info.Net.City))
            {
                try
                {
                    using HttpClient httpClient = new HttpClient();
                    string str = await httpClient.GetStringAsync($"http://ip-api.com/json").WaitAsync(TimeSpan.FromMilliseconds(30000));

                    if (string.IsNullOrWhiteSpace(str) == false)
                    {
                        Info.Net = str.DeJson<TunnelNetInfo>();
                    }
                }
                catch (Exception)
                {
                }
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
