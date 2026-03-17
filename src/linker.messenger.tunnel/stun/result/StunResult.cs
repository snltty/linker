using System.Net;

namespace linker.messenger.tunnel.stun.result;

/// <summary>
/// Base record for STUN test results, containing the mapped public endpoint and the local endpoint used for the test.
/// </summary>
public abstract record StunResult
{
	/// <summary>
	/// Gets or sets the public endpoint (mapped address) as observed by the STUN server.
	/// </summary>
	public IPEndPoint? PublicEndPoint { get; set; }

	/// <summary>
	/// Gets or sets the local endpoint used to send the STUN request.
	/// </summary>
	public IPEndPoint? LocalEndPoint { get; set; }
}
