using cmonitor.config;
using common.libs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace cmonitor.plugins.tunnel.compact
{
    public sealed class CompactTransfer
    {
        private List<ICompact> compacts;

        private readonly Config config;
        private readonly ServiceProvider serviceProvider;
        public CompactTransfer(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
        }

        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(ICompact));
            types = config.Data.Common.PluginContains(types);
            compacts = types.Select(c => (ICompact)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();

            Logger.Instance.Warning($"load tunnel compacts:{string.Join(",", compacts.Select(c => c.Name))}");
        }

        public async Task<TunnelCompactIPEndPoint[]> GetExternalIPAsync(ProtocolType protocolType)
        {
            TunnelCompactIPEndPoint[] endpoints = new TunnelCompactIPEndPoint[config.Data.Client.Tunnel.Servers.Length];

            for (int i = 0; i < config.Data.Client.Tunnel.Servers.Length; i++)
            {
                TunnelCompactInfo item = config.Data.Client.Tunnel.Servers[i];
                if (item.Disabled) continue;
                ICompact compact = compacts.FirstOrDefault(c => c.Name == item.Name);
                if (compact == null) continue;

                try
                {
                    IPEndPoint server = NetworkHelper.GetEndPoint(item.Host, 3478);
                    if (protocolType == ProtocolType.Tcp)
                    {
                        TunnelCompactIPEndPoint externalIP = await compact.GetTcpExternalIPAsync(server);
                        endpoints[i] = externalIP;
                    }
                    else if (protocolType == ProtocolType.Udp)
                    {
                        TunnelCompactIPEndPoint externalIP = await compact.GetUdpExternalIPAsync(server);
                        endpoints[i] = externalIP;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }

            return endpoints.Where(c => c != null && c.Remote != null).ToArray();
        }
    }
}
