namespace linker.messenger.tunnel.stun.enums;

/// <summary>
/// Represents the type of proxy used for STUN communication.
/// </summary>
public enum ProxyType
{
	/// <summary>
	/// No proxy; direct plain connection.
	/// </summary>
	Plain = 0,

	/// <summary>
	/// SOCKS5 proxy connection.
	/// </summary>
	Socks5 = 1
}
