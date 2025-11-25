using linker.libs;
using linker.messenger;
using linker.messenger.relay.messenger;
using linker.messenger.relay.server;
using linker.messenger.signin;
using linker.tunnel.connection;
using linker.tunnel.wanport;
using System.Security.Cryptography.X509Certificates;

namespace linker.tunnel.transport
{
    public class TransportRelay : ITunnelTransport
    {
        public string Name => "TcpRelay";

        public string Label => "TCP、服务器中继";

        public TunnelProtocolType ProtocolType => TunnelProtocolType.Tcp;

        public TunnelWanPortProtocolType AllowWanPortProtocolType => TunnelWanPortProtocolType.Other;

        public bool Reverse => true;

        public bool DisableReverse => false;

        public bool SSL => true;

        public bool DisableSSL => false;

        public byte Order => 0;

        public Action<ITunnelConnection> OnConnected { get; set; } = (state) => { };


        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly SignInClientState signInClientState;
        private readonly IMessengerStore messengerStore;

        public TransportRelay(IMessengerSender messengerSender, ISerializer serializer, SignInClientState signInClientState, IMessengerStore messengerStore)
        {
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientState = signInClientState;
            this.messengerStore = messengerStore;
        }


        public virtual async Task<ITunnelConnection> ConnectAsync(TunnelTransportInfo tunnelTransportInfo)
        {
            return null;
        }

        public virtual async Task OnBegin(TunnelTransportInfo tunnelTransportInfo)
        {

        }

        public virtual void OnFail(TunnelTransportInfo tunnelTransportInfo)
        {
        }

        public virtual void OnSuccess(TunnelTransportInfo tunnelTransportInfo)
        {
        }

        public virtual void SetSSL(X509Certificate certificate)
        {
        }

        public async Task<List<RelayServerNodeReportInfo>> RelayTestAsync()
        {
            try
            {
                MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = signInClientState.Connection,
                    MessengerId = (ushort)RelayMessengerIds.Nodes188,
                    Timeout = 2000
                }).ConfigureAwait(false);

                if (resp.Code == MessageResponeCodes.OK)
                {
                    return serializer.Deserialize<List<RelayServerNodeReportInfo>>(resp.Data.Span);
                }
            }
            catch (Exception)
            {
            }
            return new List<RelayServerNodeReportInfo>();
        }
    }
}
