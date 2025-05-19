namespace linker.messenger.tunnel.stun.messages;

public interface IStunAttributeValue
{
	int WriteTo(Span<byte> buffer);

	bool TryParse(ReadOnlySpan<byte> buffer);
}
