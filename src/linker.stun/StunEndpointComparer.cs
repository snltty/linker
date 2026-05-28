using System.Net;

namespace linker.stun;

internal static class StunEndpointComparer
{
    public static bool Equals(IPEndPoint left, IPEndPoint right)
    {
        return left.Port == right.Port && left.Address.Equals(right.Address);
    }
}
