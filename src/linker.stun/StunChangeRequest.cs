using System;

namespace linker.stun;

[Flags]
public enum StunChangeRequest
{
    None = 0,
    ChangePort = 0x02,
    ChangeIp = 0x04,
    ChangeIpAndPort = ChangeIp | ChangePort
}
