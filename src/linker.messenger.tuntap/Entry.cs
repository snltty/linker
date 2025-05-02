using linker.libs;
using linker.libs.extends;
using linker.messenger.api;
using linker.messenger.decenter;
using linker.messenger.exroute;
using linker.messenger.signin;
using linker.messenger.tunnel;
using linker.messenger.tuntap.lease;
using linker.messenger.tuntap.messenger;
using linker.tun;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
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

            serviceCollection.AddSingleton<TuntapTunnelExcludeIP>();

            serviceCollection.AddSingleton<TuntapConfigTransfer>();
            serviceCollection.AddSingleton<TuntapPingTransfer>();

            serviceCollection.AddSingleton<TuntapDecenter>();

            serviceCollection.AddSingleton<TuntapAdapter>();

            serviceCollection.AddSingleton<TuntapExRoute>();

            serviceCollection.AddSingleton<ISystemInformation, SystemInformation>();

            return serviceCollection;
        }
        public static ServiceProvider UseTuntapClient(this ServiceProvider serviceProvider, Dictionary<string, string> configDic)
        {
            if (configDic.TryGetValue("Tuntap", out string value))
            {
                ITuntapClientStore tuntapClientStore = serviceProvider.GetService<ITuntapClientStore>();
                ILeaseClientStore leaseClientStore = serviceProvider.GetService<ILeaseClientStore>();
                ISignInClientStore signInClientStore = serviceProvider.GetService<ISignInClientStore>();

                try
                {
                    JsonElement doc = JsonDocument.Parse(value).RootElement;
                    if (doc.TryGetProperty("IP", out JsonElement ip))
                    {
                        tuntapClientStore.Info.IP = IPAddress.Parse(ip.GetString());
                    }
                    if (doc.TryGetProperty("PrefixLength", out JsonElement prefixLength))
                    {
                        tuntapClientStore.Info.PrefixLength = prefixLength.GetByte();
                    }
                    if (doc.TryGetProperty("Lans", out JsonElement lans))
                    {
                        tuntapClientStore.Info.Lans = lans.GetRawText().DeJson<List<TuntapLanInfo>>();
                    }
                    if (doc.TryGetProperty("Name", out JsonElement name))
                    {
                        tuntapClientStore.Info.Name = name.GetString();
                    }
                    if (doc.TryGetProperty("Running", out JsonElement running))
                    {
                        tuntapClientStore.Info.Running = running.GetBoolean();
                    }
                    if (doc.TryGetProperty("Switch", out JsonElement _switch))
                    {
                        tuntapClientStore.Info.Switch = (TuntapSwitch)_switch.GetInt32();
                    }
                    if (doc.TryGetProperty("Forwards", out JsonElement forwards))
                    {
                        tuntapClientStore.Info.Forwards = forwards.GetRawText().DeJson<List<TuntapForwardInfo>>();
                    }
                    if (doc.TryGetProperty("Lease", out JsonElement lease))
                    {
                        leaseClientStore.Set(signInClientStore.Group.Id, lease.GetRawText().DeJson<LeaseInfo>());
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                tuntapClientStore.Confirm();
                leaseClientStore.Confirm();
            }

            TuntapProxy tuntapProxy = serviceProvider.GetService<TuntapProxy>();
            TuntapTransfer tuntapTransfer = serviceProvider.GetService<TuntapTransfer>();

            LeaseClientTreansfer leaseTreansfer = serviceProvider.GetService<LeaseClientTreansfer>();

            TuntapConfigTransfer tuntapConfigTransfer = serviceProvider.GetService<TuntapConfigTransfer>();
            TuntapPingTransfer tuntapPingTransfer = serviceProvider.GetService<TuntapPingTransfer>();

            TuntapAdapter tuntapAdapter = serviceProvider.GetService<TuntapAdapter>();


            IMessengerResolver messengerResolver = serviceProvider.GetService<IMessengerResolver>();
            messengerResolver.AddMessenger(new List<IMessenger> { serviceProvider.GetService<TuntapClientMessenger>() });

            IApiServer apiServer = serviceProvider.GetService<IApiServer>();
            apiServer.AddPlugins(new List<libs.api.IApiController> { serviceProvider.GetService<TuntapApiController>() });

            ExRouteTransfer exRouteTransfer = serviceProvider.GetService<ExRouteTransfer>();
            exRouteTransfer.AddExRoutes(new List<IExRoute> { serviceProvider.GetService<TuntapExRoute>() });

            TunnelClientExcludeIPTransfer tunnelClientExcludeIPTransfer = serviceProvider.GetService<TunnelClientExcludeIPTransfer>();
            tunnelClientExcludeIPTransfer.AddTunnelExcludeIPs(new List<ITunnelClientExcludeIP> { serviceProvider.GetService<TuntapTunnelExcludeIP>() });

            DecenterClientTransfer decenterClientTransfer = serviceProvider.GetService<DecenterClientTransfer>();
            decenterClientTransfer.AddDecenters(new List<IDecenter> { serviceProvider.GetService<TuntapDecenter>() });

            

            return serviceProvider;
        }


        public static ServiceCollection AddTuntapServer(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TuntapServerMessenger>();
            serviceCollection.AddSingleton<LeaseServerTreansfer>();

            serviceCollection.AddSingleton<ISystemInformation, SystemInformation>();

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
