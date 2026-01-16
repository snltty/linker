using System.Text.Json.Serialization;

namespace linker.messenger.listen
{
    public interface IListenStore
    {
        public int Port { get; }
        public int ApiPort { get; }

        public GeoRegistryInfo GeoRegistry { get; }

        public bool SetPort(int port);
        public bool SetApiPort(int port);
        public bool Confirm();
    }

    public sealed class GeoRegistryInfo
    {
        public string Url { get; set; } = "http://ftp.apnic.net/apnic/stats/apnic/delegated-apnic-latest";
        public string[] WhiteCountry { get; set; } = [];
        public string[] BlackCountry { get; set; } = [];

        [JsonIgnore]
        public byte[] Messengers { get; set; } = [
            (byte)ResolverType.External,
            (byte)ResolverType.Messenger,
            (byte)ResolverType.Relay,
            (byte)ResolverType.Socks4,
            (byte)ResolverType.Socks5,
        ];
    }
}
