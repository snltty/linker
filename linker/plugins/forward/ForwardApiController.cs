using linker.libs.api;
using linker.libs.extends;
using linker.client.capi;
using linker.client.config;
using System.Net;
using linker.libs;
using linker.plugins.forward.proxy;
using linker.tunnel.connection;
using System.Collections.Concurrent;

namespace linker.plugins.forward
{
    public sealed class ForwardClientApiController : IApiClientController
    {
        private readonly ForwardTransfer forwardTransfer;
        private readonly ForwardProxy forwardProxy;

        public ForwardClientApiController(ForwardTransfer forwardTransfer, ForwardProxy forwardProxy)
        {
            this.forwardTransfer = forwardTransfer;
            this.forwardProxy = forwardProxy;
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
