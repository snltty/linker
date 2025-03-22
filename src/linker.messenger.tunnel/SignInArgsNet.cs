using linker.messenger.signin.args;
using linker.messenger.signin;
using linker.libs.extends;

namespace linker.messenger.tunnel
{
    public sealed class SignInArgsNet : ISignInArgs
    {
        public string Name => "tunnelNet";
        private readonly ITunnelClientStore tunnelClientStore;
        public SignInArgsNet(ITunnelClientStore tunnelClientStore)
        {
            this.tunnelClientStore = tunnelClientStore;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("tunnelNet", new SignInArgsNetInfo { Lat = tunnelClientStore.Network.Net.Lat, Lon = tunnelClientStore.Network.Net.Lon, City = tunnelClientStore.Network.Net.City }.ToJson());

            await Task.CompletedTask.ConfigureAwait(false);

            return string.Empty;
        }

        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return string.Empty;
        }
    }
    public sealed class SignInArgsNetInfo
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string City { get; set; }
    }

}
