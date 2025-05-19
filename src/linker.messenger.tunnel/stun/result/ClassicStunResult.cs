using linker.messenger.tunnel.stun.enums;

namespace linker.messenger.tunnel.stun.result;

public record ClassicStunResult : StunResult
{
	public NatType NatType { get; set; } = NatType.Unknown;
}
