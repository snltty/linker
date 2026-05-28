using System;
using System.Net;

namespace linker.stun;

public sealed record StunBindingResult(
    StunBindingStatus Status,
    string Host,
    IPEndPoint? ServerEndPoint,
    IPEndPoint? LocalEndPoint,
    IPEndPoint? ReflexiveEndPoint,
    IPEndPoint? OtherAddress,
    IPEndPoint? ResponseOrigin,
    IPEndPoint? AlternateServer,
    StunError? Error,
    TimeSpan? Rtt,
    int Attempts,
    string? Message)
{
    public bool? IsBehindNat
    {
        get
        {
            if (Status != StunBindingStatus.Success || LocalEndPoint is null || ReflexiveEndPoint is null)
            {
                return null;
            }

            if (LocalEndPoint.Address.Equals(IPAddress.Any) || LocalEndPoint.Address.Equals(IPAddress.IPv6Any))
            {
                return null;
            }

            return !StunEndpointComparer.Equals(LocalEndPoint, ReflexiveEndPoint);
        }
    }
}
