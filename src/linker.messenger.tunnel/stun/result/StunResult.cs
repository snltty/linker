using System.Net;

namespace linker.messenger.tunnel.stun.result;

public abstract record StunResult
{
	public IPEndPoint PublicEndPoint { get; set; }
	public IPEndPoint LocalEndPoint { get; set; }
}
