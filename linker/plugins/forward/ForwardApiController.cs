using Linker.Libs.Api;
using Linker.Libs.Extends;
using Linker.Client.Capi;
using Linker.Client.Config;
using System.Net;
using Linker.Libs;

namespace Linker.Plugins.Forward
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
