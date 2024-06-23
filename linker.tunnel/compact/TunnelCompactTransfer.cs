using linker.tunnel.adapter;
using linker.libs;
using linker.libs.extends;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace linker.tunnel.compact
{
    /// <summary>
    /// 外网端口协议
    /// </summary>
    public sealed class TunnelCompactTransfer
    {
        private List<ITunnelCompact> compacts;

        private readonly ServiceProvider serviceProvider;
        private readonly ITunnelAdapter tunnelMessengerAdapter;

        public TunnelCompactTransfer(ServiceProvider serviceProvider, ITunnelAdapter tunnelMessengerAdapter)
        {
            this.serviceProvider = serviceProvider;
            this.tunnelMessengerAdapter = tunnelMessengerAdapter;
        }

        /// <summary>
        /// 加载所有外网端口协议
        /// </summary>
        /// <param name="assembs"></param>
        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs.Concat(new Assembly[] { typeof(TunnelCompactTransfer).Assembly }).ToArray(), typeof(ITunnelCompact));
            compacts = types.Select(c => (ITunnelCompact)serviceProvider.GetService(c)).Where(c => c != null).Where(c => string.IsNullOrWhiteSpace(c.Name) == false).ToList();

            Logger.Instance.Warning($"load tunnel compacts:{string.Join(",", compacts.Select(c => c.Name))}");
        }

        public List<TunnelCompactTypeInfo> GetTypes()
        {
            return compacts.Select(c => new TunnelCompactTypeInfo { Value = c.Type, Name = c.Type.ToString() }).Distinct(new TunnelCompactTypeInfoEqualityComparer()).ToList();
        }

        /// <summary>
        /// 获取外网端口
        /// </summary>
        /// <param name="localIP">你的局域网IP</param>
        /// <returns></returns>
        public async Task<TunnelCompactIPEndPoint> GetExternalIPAsync(IPAddress localIP)
        {
            var compactItems = tunnelMessengerAdapter.GetTunnelCompacts();
            foreach (TunnelCompactInfo item in compactItems)
            {
                if (item.Disabled || string.IsNullOrWhiteSpace(item.Host)) continue;
                ITunnelCompact compact = compacts.FirstOrDefault(c => c.Type == item.Type);
                if (compact == null) continue;

                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    IPEndPoint server = NetworkHelper.GetEndPoint(item.Host, 3478);
                    sw.Stop();
                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        Logger.Instance.Warning($"get domain ip time:{sw.ElapsedMilliseconds}ms");
                    }
                    TunnelCompactIPEndPoint externalIP = await compact.GetExternalIPAsync(server);
                    if (externalIP != null)
                    {
                        externalIP.Local.Address = localIP;
                        return externalIP;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
            return null;
        }
    }
}
