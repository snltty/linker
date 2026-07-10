using System;
using System.Collections.Generic;

namespace linker.discovery;

public interface IDiscoveryProtocolMatcher
{
    int GetQueryKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys);

    int GetResponseKeys(DiscoveryProtocolInfo protocol, ReadOnlySpan<byte> payload, ICollection<string> keys);
}
