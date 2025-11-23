using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.decenter;
using linker.messenger.exroute;
using linker.messenger.signin;
using linker.messenger.tunnel;
using linker.messenger.tuntap.cidr;
using linker.messenger.tuntap.lease;
using linker.messenger.tuntap.messenger;
using linker.nat;
using linker.tun;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
namespace linker.messenger.tuntap
{
    public static class Entry
    {
        public static ServiceCollection AddTuntapClient(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TuntapApiController>();
            serviceCollection.AddSingleton<LinkerTunDeviceAdapter>();
            serviceCollection.AddSingleton<TuntapTransfer>();
            serviceCollection.AddSingleton<TuntapProxy>();

            serviceCollection.AddSingleton<TuntapClientMessenger>();
            serviceCollection.AddSingleton<LeaseClientTreansfer>();

            serviceCollection.AddSingleton<TuntapCidrDecenterExcludeIP>();

            serviceCollection.AddSingleton<TuntapConfigTransfer>();
            serviceCollection.AddSingleton<TuntapPingTransfer>();

            serviceCollection.AddSingleton<TuntapDecenter>();

            serviceCollection.AddSingleton<TuntapAdapter>();

            serviceCollection.AddSingleton<TuntapExRoute>();

            serviceCollection.AddSingleton<ITuntapSystemInformation, TuntapSystemInformation>();

            serviceCollection.AddSingleton<TuntapCidrConnectionManager>();
            serviceCollection.AddSingleton<TuntapCidrDecenterManager>();
            serviceCollection.AddSingleton<TuntapCidrMapfileManager>();


            return serviceCollection;
        }
        public static ServiceProvider UseTuntapClient(this ServiceProvider serviceProvider, JsonDocument json = default)
        {
            InportConfig(serviceProvider, json);

            TuntapProxy tuntapProxy = serviceProvider.GetService<TuntapProxy>();
            TuntapTransfer tuntapTransfer = serviceProvider.GetService<TuntapTransfer>();

            LeaseClientTreansfer leaseTreansfer = serviceProvider.GetService<LeaseClientTreansfer>();

            TuntapConfigTransfer tuntapConfigTransfer = serviceProvider.GetService<TuntapConfigTransfer>();
            TuntapPingTransfer tuntapPingTransfer = serviceProvider.GetService<TuntapPingTransfer>();

            TuntapAdapter tuntapAdapter = serviceProvider.GetService<TuntapAdapter>();


            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TuntapClientMessenger>() });

            linker.messenger.api.IWebServer apiServer = serviceProvider.GetService<linker.messenger.api.IWebServer>();
            apiServer.AddPlugins(new List<IApiController> { serviceProvider.GetService<TuntapApiController>() });

            ExRouteTransfer exRouteTransfer = serviceProvider.GetService<ExRouteTransfer>();
            exRouteTransfer.AddExRoutes(new List<IExRoute> { serviceProvider.GetService<TuntapExRoute>() });

            TunnelClientExcludeIPTransfer tunnelClientExcludeIPTransfer = serviceProvider.GetService<TunnelClientExcludeIPTransfer>();
            tunnelClientExcludeIPTransfer.AddTunnelExcludeIPs(new List<ITunnelClientExcludeIP> { serviceProvider.GetService<TuntapCidrDecenterExcludeIP>() });

            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<TuntapDecenter>() });

            TuntapCidrMapfileManager tuntapCidrMapfileManager = serviceProvider.GetService<TuntapCidrMapfileManager>();

            return serviceProvider;
        }
        private static void InportConfig(ServiceProvider serviceProvider, JsonDocument json = default)
        {
            if (json != null && json.RootElement.TryGetProperty("Tuntap", out JsonElement tuntap))
            {
                ITuntapClientStore tuntapClientStore = serviceProvider.GetService<ITuntapClientStore>();
                ILeaseClientStore leaseClientStore = serviceProvider.GetService<ILeaseClientStore>();
                ISignInClientStore signInClientStore = serviceProvider.GetService<ISignInClientStore>();

                try
                {
                    if (tuntap.TryGetProperty("IP", out JsonElement ip) && tuntap.TryGetProperty("PrefixLength", out JsonElement prefixLength))
                    {
                        tuntapClientStore.Info.IP = IPAddress.Parse(ip.GetString());
                        tuntapClientStore.Info.PrefixLength = prefixLength.GetByte();

                        TuntapGroup2IPInfo tuntapGroup2IPInfo = new TuntapGroup2IPInfo { IP = tuntapClientStore.Info.IP, PrefixLength= tuntapClientStore.Info.PrefixLength };
                        tuntapClientStore.Info.Group2IP[signInClientStore.Group.Id] = tuntapGroup2IPInfo;
                           // .AddOrUpdate(signInClientStore.Group.Id, tuntapGroup2IPInfo,(a,b)=> tuntapGroup2IPInfo);
                    }
                    if (tuntap.TryGetProperty("Lans", out JsonElement lans))
                    {
                        tuntapClientStore.Info.Lans = lans.GetRawText().DeJson<List<TuntapLanInfo>>();
                    }
                    if (tuntap.TryGetProperty("Name", out JsonElement name))
                    {
                        tuntapClientStore.Info.Name = name.GetString();
                    }
                    if (tuntap.TryGetProperty("Running", out JsonElement running))
                    {
                        tuntapClientStore.Info.Running = running.GetBoolean();
                    }
                    if (tuntap.TryGetProperty("Switch", out JsonElement _switch))
                    {
                        tuntapClientStore.Info.Switch = (TuntapSwitch)_switch.GetInt32();
                    }
                    if (tuntap.TryGetProperty("Forwards", out JsonElement forwards))
                    {
                        tuntapClientStore.Info.Forwards = forwards.GetRawText().DeJson<List<TuntapForwardInfo>>();
                    }
                    if (tuntap.TryGetProperty("Lease", out JsonElement lease))
                    {
                        leaseClientStore.Set(signInClientStore.Group.Id, lease.GetRawText().DeJson<LeaseInfo>());
                    }
                    if (tuntap.TryGetProperty("Guid", out JsonElement guid))
                    {
                        tuntapClientStore.Info.Guid = Guid.Parse(guid.GetRawText());
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                tuntapClientStore.Confirm();
                leaseClientStore.Confirm();
            }
        }


        public static ServiceCollection AddTuntapServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TuntapServerMessenger>();
            serviceCollection.AddSingleton<LeaseServerTreansfer>();

            serviceCollection.AddSingleton<ITuntapSystemInformation, TuntapSystemInformation>();

            return serviceCollection;
        }
        public static ServiceProvider UseTuntapServer(this ServiceProvider serviceProvider)
        {
            LeaseServerTreansfer leaseTreansfer = serviceProvider.GetService<LeaseServerTreansfer>();

            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TuntapServerMessenger>() });

            return serviceProvider;
        }
    }
}
