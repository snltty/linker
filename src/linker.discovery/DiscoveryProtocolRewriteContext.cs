using System;
using System.Collections.Generic;
using System.Net;

namespace linker.discovery;

public sealed class DiscoveryProtocolRewriteContext
{
    private readonly IReadOnlyList<DiscoveryAddressMapEntry> _addressMaps;
    private readonly Action<IPAddress, IPAddress>? _addressRewritten;
    private readonly Action<string, string, string>? _payloadRewritten;

    internal DiscoveryProtocolRewriteContext(
        DiscoveryProtocolInfo protocol,
        IReadOnlyList<DiscoveryAddressMapEntry> addressMaps,
        Action<IPAddress, IPAddress>? addressRewritten,
        Action<string, string, string>? payloadRewritten)
    {
        Protocol = protocol;
        _addressMaps = addressMaps;
        _addressRewritten = addressRewritten;
        _payloadRewritten = payloadRewritten;
    }

    public DiscoveryProtocolInfo Protocol { get; }

    public bool HasAddressMaps => _addressMaps.Count > 0;

    public bool TryMapRealToMapped(IPAddress realAddress, out IPAddress mappedAddress)
    {
        ArgumentNullException.ThrowIfNull(realAddress);

        foreach (DiscoveryAddressMapEntry map in _addressMaps)
        {
            if (map.TryMapRealToMapped(realAddress, out mappedAddress))
            {
                return true;
            }
        }

        mappedAddress = IPAddress.None;
        return false;
    }

    public bool TryMapRealToMapped(ReadOnlySpan<byte> realAddress, Span<byte> mappedAddress)
    {
        foreach (DiscoveryAddressMapEntry map in _addressMaps)
        {
            if (map.TryMapRealToMapped(realAddress, mappedAddress))
            {
                return true;
            }
        }

        return false;
    }

    public void ReportAddressRewrite(IPAddress originalAddress, IPAddress mappedAddress)
    {
        _addressRewritten?.Invoke(originalAddress, mappedAddress);
    }

    public void ReportPayloadRewrite(string fieldName, string originalValue, string rewrittenValue)
    {
        _payloadRewritten?.Invoke(fieldName, originalValue, rewrittenValue);
    }
}
