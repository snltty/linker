using System.Net.Sockets;

namespace linker.stun;

internal static class StunP2PSuccessEstimator
{
    public static StunP2PEstimate? Estimate(
        StunBindingResult binding,
        StunNatMappingBehavior mappingBehavior,
        StunNatFilteringBehavior filteringBehavior)
    {
        if (binding.Status != StunBindingStatus.Success)
        {
            return null;
        }

        if (binding.ReflexiveEndPoint?.Address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return new StunP2PEstimate(100, "IPv6 public endpoint; direct P2P is expected to work if host firewall policy allows it.");
        }

        if (mappingBehavior == StunNatMappingBehavior.NotNated)
        {
            return new StunP2PEstimate(100, "No NAT was detected; direct P2P is expected to work if host firewall policy allows it.");
        }

        if (mappingBehavior == StunNatMappingBehavior.Unknown || filteringBehavior == StunNatFilteringBehavior.Unknown)
        {
            return null;
        }

        return (mappingBehavior, filteringBehavior) switch
        {
            (StunNatMappingBehavior.EndpointIndependent, StunNatFilteringBehavior.EndpointIndependent) =>
                new StunP2PEstimate(95, "Endpoint-independent mapping and filtering are highly favorable for UDP hole punching."),

            (StunNatMappingBehavior.EndpointIndependent, StunNatFilteringBehavior.AddressDependent) =>
                new StunP2PEstimate(85, "Endpoint-independent mapping is favorable; address-dependent filtering usually works with coordinated simultaneous sends."),

            (StunNatMappingBehavior.EndpointIndependent, StunNatFilteringBehavior.AddressAndPortDependent) =>
                new StunP2PEstimate(70, "Endpoint-independent mapping preserves the public endpoint, but address-and-port filtering requires accurate simultaneous punching."),

            (StunNatMappingBehavior.AddressDependent, StunNatFilteringBehavior.EndpointIndependent) =>
                new StunP2PEstimate(75, "Filtering is permissive, but address-dependent mapping may change the public endpoint for different peer addresses."),

            (StunNatMappingBehavior.AddressDependent, StunNatFilteringBehavior.AddressDependent) =>
                new StunP2PEstimate(60, "Both mapping and filtering depend on peer address; punching can work with coordination but is less reliable."),

            (StunNatMappingBehavior.AddressDependent, StunNatFilteringBehavior.AddressAndPortDependent) =>
                new StunP2PEstimate(45, "Address-dependent mapping plus strict filtering makes direct punching fragile."),

            (StunNatMappingBehavior.AddressAndPortDependent, StunNatFilteringBehavior.EndpointIndependent) =>
                new StunP2PEstimate(35, "Address-and-port-dependent mapping is symmetric-NAT-like; direct P2P often needs relay fallback."),

            (StunNatMappingBehavior.AddressAndPortDependent, StunNatFilteringBehavior.AddressDependent) =>
                new StunP2PEstimate(20, "Symmetric-NAT-like mapping with address-dependent filtering has a low direct punching success chance."),

            (StunNatMappingBehavior.AddressAndPortDependent, StunNatFilteringBehavior.AddressAndPortDependent) =>
                new StunP2PEstimate(10, "Symmetric-NAT-like mapping and strict filtering usually require a relay."),

            _ => null
        };
    }
}
