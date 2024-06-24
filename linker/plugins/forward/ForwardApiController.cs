using linker.libs.api;
using linker.libs.extends;
using linker.client.capi;
using linker.client.config;
using System.Net;
using linker.libs;

namespace linker.plugins.forward
{
    public sealed class ForwardClientApiController : IApiClientController
    {
        private readonly ForwardTransfer forwardTransfer;

        public ForwardClientApiController(ForwardTransfer forwardTransfer)
        {
            this.forwardTransfer = forwardTransfer;
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
