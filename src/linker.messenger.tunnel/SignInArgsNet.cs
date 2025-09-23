using linker.messenger.signin.args;
using linker.libs.extends;

namespace linker.messenger.tunnel
{
    public sealed class SignInArgsNet : ISignInArgsClient
    {
        public string Name => "tunnelNet";
        public SignInArgsLevel Level => SignInArgsLevel.Default;

        private readonly ITunnelClientStore tunnelClientStore;
        public SignInArgsNet(ITunnelClientStore tunnelClientStore)
        {
            this.tunnelClientStore = tunnelClientStore;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("tunnelNet", new SignInArgsNetInfo
            {
                Lat = tunnelClientStore.Network.Net.Lat,
                Lon = tunnelClientStore.Network.Net.Lon,
                City = tunnelClientStore.Network.Net.City,
            }.ToJson());
            return await Task.FromResult(string.Empty);
        }
    }
    public sealed class SignInArgsNetInfo
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string City { get; set; }
    }

}
