namespace linker.stun;

public enum StunNatBehaviorStatus
{
    Success,
    UdpBlocked,
    Rfc5780NotSupported,
    ResolveFailed,
    ProtocolError,
    SocketError
}
