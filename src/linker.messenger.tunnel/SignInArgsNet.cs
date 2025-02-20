using linker.messenger.signin.args;
using linker.messenger.signin;
using linker.libs.extends;

namespace linker.messenger.tunnel
{
    public sealed class SignInArgsNet : ISignInArgs
    {
        public string Name => "tunnelNet";
        private readonly TunnelNetworkTransfer tunnelNetworkTransfer;
        public SignInArgsNet(TunnelNetworkTransfer tunnelNetworkTransfer)
        {
            this.tunnelNetworkTransfer = tunnelNetworkTransfer;
        }
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            args.TryAdd("tunnelNet", new SignInArgsNetInfo { Lat = tunnelNetworkTransfer.Info.Net.Lat, Lon = tunnelNetworkTransfer.Info.Net.Lon, City = tunnelNetworkTransfer.Info.Net.City }.ToJson());

            await Task.CompletedTask;

            return string.Empty;
        }

        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            await Task.CompletedTask;
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
