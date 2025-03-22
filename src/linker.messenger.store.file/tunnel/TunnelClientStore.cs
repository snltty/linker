using linker.tunnel.transport;
using linker.messenger.signin;
using linker.messenger.tunnel;

namespace linker.messenger.store.file.tunnel
{
    public sealed class TunnelClientStore : ITunnelClientStore
    {
        public int RouteLevelPlus => runningConfig.Data.Tunnel.RouteLevelPlus;

        public int PortMapPrivate => runningConfig.Data.Tunnel.PortMapLan;
        public int PortMapPublic => runningConfig.Data.Tunnel.PortMapWan;
        public TunnelPublicNetworkInfo Network => runningConfig.Data.Tunnel.Network;

        public Action OnChanged { get; set; } = () => { };

        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly FileConfig config;
        private readonly RunningConfig runningConfig;

        public TunnelClientStore(SignInClientState signInClientState, ISignInClientStore signInClientStore, FileConfig config, RunningConfig runningConfig)
        {
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.config = config;
            this.runningConfig = runningConfig;
        }
        public async Task<bool> SetTunnelTransports(List<TunnelTransportItemInfo> list)
        {
            config.Data.Client.Tunnel.Transports = list;
            config.Data.Update();

            OnChanged();

            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public async Task<List<TunnelTransportItemInfo>> GetTunnelTransports()
        {
            return await Task.FromResult(config.Data.Client.Tunnel.Transports).ConfigureAwait(false);
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
            return await Task.FromResult(true).ConfigureAwait(false);
        }
    }
}
