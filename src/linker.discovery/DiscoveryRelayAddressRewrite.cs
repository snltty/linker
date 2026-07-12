using System.Net;

namespace linker.discovery;

public sealed class DiscoveryRelayAddressRewrite
{
    public DiscoveryRelayAddressRewrite(
        DiscoveryProtocolInfo protocol,
        string direction,
        IPAddress originalAddress,
        IPAddress mappedAddress)
    {
        Protocol = protocol;
        Direction = direction;
        OriginalAddress = originalAddress;
        MappedAddress = mappedAddress;
    }

    public DiscoveryProtocolInfo Protocol { get; }

    public string Direction { get; }

    public IPAddress OriginalAddress { get; }

    public IPAddress MappedAddress { get; }
}
