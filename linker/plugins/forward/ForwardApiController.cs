using linker.libs.api;
using linker.libs.extends;
using linker.client.capi;
using linker.client.config;
using System.Net;
using linker.libs;
using linker.plugins.forward.proxy;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using linker.plugins.forward.messenger;
using linker.server;
using linker.client;
using MemoryPack;

namespace linker.plugins.forward
{
    public sealed class ForwardClientApiController : IApiClientController
    {
        private readonly ForwardTransfer forwardTransfer;
        private readonly ForwardProxy forwardProxy;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public ForwardClientApiController(ForwardTransfer forwardTransfer, ForwardProxy forwardProxy, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.forwardTransfer = forwardTransfer;
            this.forwardProxy = forwardProxy;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
        }

        public ConcurrentDictionary<string, ITunnelConnection> Connections(ApiControllerParamsInfo param)
        {
            return forwardProxy.GetConnections();
        }
        public void RemoveConnection(ApiControllerParamsInfo param)
        {
            forwardProxy.RemoveConnection(param.Content);
        }

        public void TestListen(ApiControllerParamsInfo param)
        {
            forwardTransfer.TestListen();
        }
        public void TestTarget(ApiControllerParamsInfo param)
        {
            if (string.IsNullOrWhiteSpace(param.Content))
            {
                forwardTransfer.TestTarget();
            }
            else
            {
                forwardTransfer.TestTarget(param.Content);
            }

        }

        public Dictionary<string, List<ForwardInfo>> Get(ApiControllerParamsInfo param)
        {
            return forwardTransfer.Get();
        }
        public async Task<List<ForwardRemoteInfo>> GetRemote(ApiControllerParamsInfo param)
        {
            GetForwardInfo request = param.Content.DeJson<GetForwardInfo>();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)ForwardMessengerIds.GetForward,
                Payload = MemoryPackSerializer.Serialize(request)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return MemoryPackSerializer.Deserialize<List<ForwardRemoteInfo>>(resp.Data.Span);
            }
            return new List<ForwardRemoteInfo>();
        }

        public IPAddress[] BindIPs(ApiControllerParamsInfo param)
        {
            return NetworkHelper.GetIPV4();
        }

        public bool Add(ApiControllerParamsInfo param)
        {
            ForwardInfo info = param.Content.DeJson<ForwardInfo>();
            return forwardTransfer.Add(info);
        }
        public bool Remove(ApiControllerParamsInfo param)
        {
            if (uint.TryParse(param.Content, out uint id))
            {
                return forwardTransfer.Remove(id);
            }
            return false;
        }
    }
}
