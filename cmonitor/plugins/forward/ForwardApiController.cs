using common.libs.api;
using cmonitor.config;
using common.libs.extends;
using cmonitor.client.capi;

namespace cmonitor.plugins.forward
{
    public sealed class ForwardClientApiController : IApiClientController
    {
        private readonly ForwardTransfer forwardTransfer;

        public ForwardClientApiController(ForwardTransfer forwardTransfer)
        {
            this.forwardTransfer = forwardTransfer;
        }

        public bool Add(ApiControllerParamsInfo param)
        {
            ForwardInfo info = param.Content.DeJson<ForwardInfo>();
            return forwardTransfer.AddForward(info);
        }

        public bool Remove(ApiControllerParamsInfo param)
        {
            if (uint.TryParse(param.Content, out uint id))
            {
                return forwardTransfer.RemoveForward(id);
            }
            return false;
        }
    }
}
