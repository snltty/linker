using System.Collections.Generic;
using System.Net;

namespace linker.stun;

public sealed record StunMessage(
    ushort MessageType,
    byte[] TransactionId,
    IReadOnlyList<StunAttribute> Attributes,
    IPEndPoint? XorMappedAddress,
    IPEndPoint? MappedAddress,
    IPEndPoint? OtherAddress,
    IPEndPoint? ResponseOrigin,
    IPEndPoint? AlternateServer,
    StunError? Error)
{
    public IPEndPoint? ReflexiveEndPoint => XorMappedAddress ?? MappedAddress;
}
