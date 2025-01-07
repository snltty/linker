using linker.libs;
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

        public TunnelNetworkTransfer(ISignInClientStore signInClientStore, SignInClientState signInClientState,TunnelTransfer tunnelTransfer)
        {
            this.signInClientStore = signInClientStore;

            signInClientState.NetworkEnabledHandleBefore += () =>
            {
                RefreshRouteLevel();
                tunnelTransfer.Refresh();
            };
            TestQuic();
        }
        /// <summary>
        /// 刷新网关等级数据
        /// </summary>
        private void RefreshRouteLevel()
        {
            Info.RouteLevel = NetworkHelper.GetRouteLevel(signInClientStore.Server.Host, out List<IPAddress> ips);
            Info.RouteIPs = ips.ToArray();
            Info.LocalIPs = NetworkHelper.GetIPV6().Concat(NetworkHelper.GetIPV4()).ToArray();
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
        }
    }
}
