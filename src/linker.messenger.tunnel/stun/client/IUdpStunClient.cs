namespace linker.messenger.tunnel.stun.client;

public interface IUdpStunClient : IStunClient
{
	TimeSpan ReceiveTimeout { get; set; }
}
