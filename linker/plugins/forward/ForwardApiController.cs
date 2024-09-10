using linker.libs.api;
using linker.libs.extends;
using linker.client.config;
using System.Net;
using linker.libs;
using linker.plugins.forward.proxy;
using linker.tunnel.connection;
using System.Collections.Concurrent;
using linker.plugins.forward.messenger;
using MemoryPack;
using linker.plugins.client;
using linker.plugins.capi;
using linker.plugins.messenger;
using linker.config;

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

        public ConnectionListInfo Connections(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (forwardProxy.Version.Eq(hashCode, out ulong version) == false)
            {
                return new ConnectionListInfo
                {
                    List = forwardProxy.GetConnections(),
                    HashCode = version
                };
            }
            return new ConnectionListInfo { HashCode = version };
        }

        [ClientApiAccessAttribute(ClientApiAccess.TunnelRemove)]
        public void RemoveConnection(ApiControllerParamsInfo param)
        {
            forwardProxy.RemoveConnection(param.Content);
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

        public ForwardListInfo Get(ApiControllerParamsInfo param)
        {
            ulong hashCode = ulong.Parse(param.Content);
            if (forwardTransfer.Version.Eq(hashCode, out ulong version) == false)
            {
                return new ForwardListInfo
                {
                    List = forwardTransfer.Get(),
                    HashCode = version
                };
            }
            return new ForwardListInfo { HashCode = version };
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

        [ClientApiAccessAttribute(ClientApiAccess.ForwardSelf)]
        public bool Add(ApiControllerParamsInfo param)
        {
            ForwardInfo info = param.Content.DeJson<ForwardInfo>();
            return forwardTransfer.Add(info);
        }

        [ClientApiAccessAttribute(ClientApiAccess.ForwardSelf)]
        public bool Remove(ApiControllerParamsInfo param)
        {
            if (uint.TryParse(param.Content, out uint id))
            {
                return forwardTransfer.Remove(id);
            }
            return false;
        }
    }

    public sealed class ForwardListInfo
    {
        public Dictionary<string, List<ForwardInfo>> List { get; set; }
        public ulong HashCode { get; set; }
    }
    public sealed class ConnectionListInfo
    {
        public ConcurrentDictionary<string, ITunnelConnection> List { get; set; }
        public ulong HashCode { get; set; }
    }
}
