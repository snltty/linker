using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.client;
using MemoryPack;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Reflection;

namespace linker.plugins.tunnel.excludeip
{
    public sealed class TunnelExcludeIPTransfer
    {
        private List<ITunnelExcludeIP> excludeIPs;
        private string exipConfigKey = "excludeIPConfig";

        private readonly RunningConfig running;
        private readonly ClientSignInState clientSignInState;
        private readonly RunningConfigTransfer runningConfigTransfer;
        private readonly FileConfig fileConfig;

        private readonly ServiceProvider serviceProvider;
        public TunnelExcludeIPTransfer(RunningConfig running, ClientSignInState clientSignInState, RunningConfigTransfer runningConfigTransfer, FileConfig fileConfig, ServiceProvider serviceProvider)
        {
            this.running = running;
            this.clientSignInState = clientSignInState;
            this.runningConfigTransfer = runningConfigTransfer;
            this.fileConfig = fileConfig;
            this.serviceProvider = serviceProvider;
            InitExcludeIP();
        }

        public void Load(Assembly[] assembs)
        {
            IEnumerable<Type> types = ReflectionHelper.GetInterfaceSchieves(assembs, typeof(ITunnelExcludeIP));
            excludeIPs = types.Select(c => (ITunnelExcludeIP)serviceProvider.GetService(c)).Where(c => c != null).ToList();

            LoggerHelper.Instance.Info($"load tunnel excludeips :{string.Join(",", types.Select(c => c.Name))}");
        }

        public List<ExcludeIPItem> Get()
        {
            List<ExcludeIPItem> result = new List<ExcludeIPItem>();
            foreach (var item in excludeIPs)
            {
                var ips = item.Get();
                if (ips != null && ips.Length > 0)
                {
                    result.AddRange(ips);
                }
            }
            if (running.Data.Tunnel.ExcludeIPs.Length > 0)
            {
                result.AddRange(running.Data.Tunnel.ExcludeIPs);
            }
            
            return result;
        }

        private void InitExcludeIP()
        {
            clientSignInState.NetworkEnabledHandle += (times) => SyncExcludeIP();
            runningConfigTransfer.Setter(exipConfigKey, SettExcludeIPs);
            runningConfigTransfer.Getter(exipConfigKey, () => MemoryPackSerializer.Serialize(GetExcludeIPs()));
        }
        private void SyncExcludeIP()
        {
            runningConfigTransfer.Sync(exipConfigKey, MemoryPackSerializer.Serialize(running.Data.Tunnel.ExcludeIPs));
        }
        public ExcludeIPItem[] GetExcludeIPs()
        {
            return running.Data.Tunnel.ExcludeIPs;
        }
        public void SettExcludeIPs(ExcludeIPItem[] ips)
        {
            running.Data.Tunnel.ExcludeIPs = ips;
            running.Data.Update();
            runningConfigTransfer.IncrementVersion(exipConfigKey);
            SyncExcludeIP();
        }
        private void SettExcludeIPs(Memory<byte> data)
        {
            running.Data.Tunnel.ExcludeIPs = MemoryPackSerializer.Deserialize<ExcludeIPItem[]>(data.Span);
            running.Data.Update();
        }
    }
}
