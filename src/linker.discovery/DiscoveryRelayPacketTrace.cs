using System.Net;

namespace linker.discovery;

public sealed class DiscoveryRelayPacketTrace
{
    public DiscoveryRelayPacketTrace(
        DiscoveryProtocolInfo protocol,
        string direction,
        IPAddress localAddress,
        EndPoint? remoteEndPoint,
        EndPoint? targetEndPoint,
        int bytes,
        string remark)
    {
        Protocol = protocol;
        Direction = direction;
        LocalAddress = localAddress;
        RemoteEndPoint = remoteEndPoint;
        TargetEndPoint = targetEndPoint;
        Bytes = bytes;
        Remark = remark;
    }

    public DiscoveryProtocolInfo Protocol { get; }

    public string Direction { get; }

    public IPAddress LocalAddress { get; }

    public EndPoint? RemoteEndPoint { get; }

    public EndPoint? TargetEndPoint { get; }

    public int Bytes { get; }

    public string Remark { get; }
}
