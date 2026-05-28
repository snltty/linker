namespace linker.stun;

public enum StunBindingStatus
{
    Success,
    TimedOut,
    ResolveFailed,
    ProtocolError,
    ServerError,
    SocketError,
    UnsupportedAddressFamily
}
