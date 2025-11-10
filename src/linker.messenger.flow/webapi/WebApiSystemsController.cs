using linker.libs.extends;
using linker.libs.web;
namespace linker.messenger.flow.webapi
{
    public sealed class WebApiSystemsController : IWebApiController
    {
        public string Path => "/flow/systems.json";

        private readonly FlowResolver flowResolver;
        public WebApiSystemsController(FlowResolver flowResolver)
        {
            this.flowResolver = flowResolver;
        }
        public Memory<byte> Handle(string query)
        {
            return flowResolver.GetSystems().ToJson().ToBytes();
        }

        public void Free()
        {
        }
    }
}
