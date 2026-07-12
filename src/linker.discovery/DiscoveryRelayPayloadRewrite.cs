namespace linker.discovery;

public sealed class DiscoveryRelayPayloadRewrite
{
    public DiscoveryRelayPayloadRewrite(
        DiscoveryProtocolInfo protocol,
        string direction,
        string fieldName,
        string originalValue,
        string rewrittenValue)
    {
        Protocol = protocol;
        Direction = direction;
        FieldName = fieldName;
        OriginalValue = originalValue;
        RewrittenValue = rewrittenValue;
    }

    public DiscoveryProtocolInfo Protocol { get; }

    public string Direction { get; }

    public string FieldName { get; }

    public string OriginalValue { get; }

    public string RewrittenValue { get; }
}
