using linker.messenger.tunnel.stun.enums;
using System.Net;

namespace linker.messenger.tunnel.stun.proxy;

/// <summary>
/// Factory for creating TCP and UDP proxy instances based on transport and proxy type configuration.
/// </summary>
public static class ProxyFactory
{
	/// <summary>
	/// Creates a UDP proxy instance for the specified transport and proxy type.
	/// </summary>
	/// <param name="transport">The transport type (UDP or DTLS).</param>
	/// <param name="type">The proxy type (Plain or SOCKS5).</param>
	/// <param name="local">The local endpoint to bind to.</param>
	/// <param name="option">The SOCKS5 connection options, required when proxy type is SOCKS5.</param>
	/// <param name="targetHost">The target host name, used for DTLS server name indication.</param>
	/// <param name="skipCertificateValidation">Whether to skip server certificate validation for DTLS.</param>
	/// <returns>A configured <see cref="IUdpProxy"/> instance.</returns>
	public static IUdpProxy CreateProxy(TransportType transport, ProxyType type, IPEndPoint local, string targetHost, bool skipCertificateValidation = false)
	{
		return (transport, type) switch
		{
			(TransportType.Udp, ProxyType.Plain) => new NoneUdpProxy(local),
			_ => throw new NotSupportedException()
		};
	}
}
