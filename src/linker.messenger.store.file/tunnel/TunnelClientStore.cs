using linker.libs;
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
        }
        public async Task<bool> SetTunnelTransports(string machineId, List<TunnelTransportItemInfo> list)
        {
            if (string.IsNullOrWhiteSpace(machineId)) return false;

            runningConfig.Data.Tunnel.Transports[machineId] = list;
            //   .AddOrUpdate(machineId, list, (a, b) => list);
            runningConfig.Data.Update();

            OnChanged();

            return await Task.FromResult(true).ConfigureAwait(false);
        }
        public async Task<bool> SetTunnelTransports(string machineId, List<ITunnelTransport> list)
        {
            if (string.IsNullOrWhiteSpace(machineId)) return false;

            if (runningConfig.Data.Tunnel.Transports.TryGetValue(machineId, out List<TunnelTransportItemInfo> transportItems) == false)
            {
                transportItems = new List<TunnelTransportItemInfo>();
            }

            Rebuild(transportItems, list);
            ForceUpdate(transportItems, list);

            runningConfig.Data.Tunnel.Transports[machineId] = transportItems;
            //.AddOrUpdate(machineId, transportItems, (a, b) => transportItems);
            runningConfig.Data.Update();

            OnChanged();

            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async Task<List<TunnelTransportItemInfo>> GetTunnelTransports(string machineId)
        {
            if (runningConfig.Data.Tunnel.Transports.TryGetValue(machineId, out List<TunnelTransportItemInfo> list))
            {
                if (runningConfig.Data.Tunnel.Transports.TryGetValue(Helper.GlobalString, out List<TunnelTransportItemInfo> defaults))
                {
                    if (Rebuild(list, defaults))
                    {
                        runningConfig.Data.Tunnel.Transports[machineId] = list;
                        //.AddOrUpdate(machineId, list, (a, b) => list);
                        runningConfig.Data.Update();
                    }
                }
                return list;
            }
            if (runningConfig.Data.Tunnel.Transports.TryGetValue(Helper.GlobalString, out list))
            {
                return list;
            }
            return await Task.FromResult(new List<TunnelTransportItemInfo>()).ConfigureAwait(false);
        }

        private void ForceUpdate(List<TunnelTransportItemInfo> currents, List<ITunnelTransport> news)
        {
            //强制更新一些信息
            foreach (var item in currents)
            {
                var transport = news.FirstOrDefault(c => c.Name == item.Name);
                if (transport != null)
                {
                    item.DisableReverse = transport.DisableReverse;
                    item.DisableSSL = transport.DisableSSL;
                    item.Name = transport.Name;
                    item.Label = transport.Label;
                    if (transport.DisableReverse)
                    {
                        item.Reverse = transport.Reverse;
                    }
                    if (transport.DisableSSL)
                    {
                        item.SSL = transport.SSL;
                    }
                    if (item.Order == 0)
                    {
                        item.Order = transport.Order;
                    }
                }
            }

        }
        private bool Rebuild(List<TunnelTransportItemInfo> currents, List<ITunnelTransport> news)
        {
            return Rebuild(currents, news.Select(c => new TunnelTransportItemInfo
            {
                Label = c.Label,
                Name = c.Name,
                ProtocolType = c.ProtocolType.ToString(),
                Reverse = c.Reverse,
                DisableReverse = c.DisableReverse,
                SSL = c.SSL,
                DisableSSL = c.DisableSSL,
                Order = c.Order
            }).ToList());
        }
        private bool Rebuild(List<TunnelTransportItemInfo> currents, List<TunnelTransportItemInfo> news)
        {
            //有新的协议
            var newTransportNames = news.Select(c => c.Name).Except(currents.Select(c => c.Name));
            if (newTransportNames.Any())
            {
                currents.AddRange(news.Where(c => newTransportNames.Contains(c.Name)).Select(c => new TunnelTransportItemInfo
                {
                    Label = c.Label,
                    Name = c.Name,
                    ProtocolType = c.ProtocolType.ToString(),
                    Reverse = c.Reverse,
                    DisableReverse = c.DisableReverse,
                    SSL = c.SSL,
                    DisableSSL = c.DisableSSL,
                    Order = c.Order
                }));
            }
            //有已移除的协议
            var oldTransportNames = currents.Select(c => c.Name).Except(news.Select(c => c.Name));
            if (oldTransportNames.Any())
            {
                foreach (var item in currents.Where(c => oldTransportNames.Contains(c.Name)).ToList())
                {
                    currents.Remove(item);
                }
            }
            return newTransportNames.Any() || oldTransportNames.Any();
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
