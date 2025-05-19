namespace linker.messenger.tunnel.stun.enums;

public enum FilteringBehavior
{
	Unknown,
	UnsupportedServer,
	EndpointIndependent,
	AddressDependent,
	AddressAndPortDependent,

	/// <summary>
	/// Filtering test applies only to UDP.
	/// </summary>
	None
}
