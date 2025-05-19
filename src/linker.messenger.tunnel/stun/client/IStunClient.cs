namespace linker.messenger.tunnel.stun.client;

public interface IStunClient : IDisposable
{
	ValueTask QueryAsync(CancellationToken cancellationToken = default);
}
