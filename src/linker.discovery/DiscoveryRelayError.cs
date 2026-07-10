using System;
using System.Net;

namespace linker.discovery;

public sealed class DiscoveryRelayError
{
    public DiscoveryRelayError(
        DiscoveryProtocolInfo protocol,
        string direction,
        IPAddress localAddress,
        EndPoint? remoteEndPoint,
        Exception exception)
    {
        Protocol = protocol;
        Direction = direction;
        LocalAddress = localAddress;
        RemoteEndPoint = remoteEndPoint;
        Exception = exception;
    }

    public DiscoveryProtocolInfo Protocol { get; }

    public string Direction { get; }

    public IPAddress LocalAddress { get; }

    public EndPoint? RemoteEndPoint { get; }

    public Exception Exception { get; }
}
