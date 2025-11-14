using linker.messenger.tunnel;
using linker.tunnel.transport;

namespace linker.messenger.store.file.tunnel
{
    public sealed class TunnelClientStore : ITunnelClientStore
    {
        public int RouteLevelPlus => runningConfig.Data.Tunnel.RouteLevelPlus;

        public int PortMapPrivate => runningConfig.Data.Tunnel.PortMapLan;
        public int PortMapPublic => runningConfig.Data.Tunnel.PortMapWan;
        public TunnelPublicNetworkInfo Network => runningConfig.Data.Tunnel.Network;

        public Action OnChanged { get; set; } = () => { };

        public int TransportMachineIdCount => runningConfig.Data.Tunnel.Transports.Count;

        private readonly RunningConfig runningConfig;

        public TunnelClientStore(FileConfig config, RunningConfig runningConfig)
        {
            this.runningConfig = runningConfig;

            var list = config.Data.Client.Tunnel.Transports;
            if (list != null && list.Count > 0)
            {

                runningConfig.Data.Tunnel.Transports.AddOrUpdate("default", list, (a, b) => list);
                runningConfig.Data.Update();

                config.Data.Client.Tunnel.Transports = [];
                config.Data.Update();
            }
        }
        public async Task<bool> SetTunnelTransports(string machineId, List<TunnelTransportItemInfo> list)
        {
            if (string.IsNullOrWhiteSpace(machineId)) return false;

            runningConfig.Data.Tunnel.Transports.AddOrUpdate(machineId, list, (a, b) => list);
            runningConfig.Data.Update();

            OnChanged();

            return true;
        }

        public async Task<List<TunnelTransportItemInfo>> GetTunnelTransports(string machineId)
        {
            if (runningConfig.Data.Tunnel.Transports.TryGetValue(machineId, out List<TunnelTransportItemInfo> list))
            {
                return list;
            }
            if (runningConfig.Data.Tunnel.Transports.TryGetValue("default", out list))
            {
                return list;
            }
            return [];
        }

        public async Task<bool> SetRouteLevelPlus(int level)
        {
            runningConfig.Data.Tunnel.RouteLevelPlus = level;
            runningConfig.Data.Update();
            OnChanged();
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async Task<bool> SetPortMap(int privatePort, int publicPort)
        {
            runningConfig.Data.Tunnel.PortMapLan = privatePort;
            runningConfig.Data.Tunnel.PortMapWan = publicPort;
            runningConfig.Data.Update();
            OnChanged();
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async Task<bool> SetNetwork(TunnelPublicNetworkInfo network)
        {
            runningConfig.Data.Update();
            OnChanged();
            return false;
        }
    }
}
