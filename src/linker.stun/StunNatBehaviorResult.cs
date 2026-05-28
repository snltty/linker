namespace linker.stun;

using System.Net.Sockets;

public sealed record StunNatBehaviorResult(
    StunNatBehaviorStatus Status,
    StunBindingResult Binding,
    StunNatMappingBehavior MappingBehavior,
    StunNatFilteringBehavior FilteringBehavior,
    StunBindingResult? MappingTest2,
    StunBindingResult? MappingTest3,
    StunBindingResult? FilteringTest2,
    StunBindingResult? FilteringTest3,
    string? Message)
{
    public StunP2PEstimate? P2PEstimate => StunP2PSuccessEstimator.Estimate(Binding, MappingBehavior, FilteringBehavior);

    public int? EstimatedP2PSuccessRate => P2PEstimate?.SuccessRate;

    public string? EstimatedP2PSuccessReason => P2PEstimate?.Reason;

    public string P2PSummary
    {
        get
        {
            var addressFamily = Binding.ReflexiveEndPoint?.Address.AddressFamily switch
            {
                AddressFamily.InterNetwork => "IPV4",
                AddressFamily.InterNetworkV6 => "IPV6",
                _ => "UNKNOWN"
            };

            return $"{MappingBehavior}/{FilteringBehavior}/{addressFamily}-{EstimatedP2PSuccessRate ?? 0}%";
        }
    }
}
