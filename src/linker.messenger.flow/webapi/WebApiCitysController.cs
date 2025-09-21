using linker.libs.extends;
using linker.libs.web;
using linker.messenger.signin;
namespace linker.messenger.flow.webapi
{
    public sealed class WebApiCitysController : IWebApiController
    {
        public string Path => "/flow/citys.json";

        private readonly FlowResolver flowResolver;
        public WebApiCitysController(FlowResolver flowResolver)
        {
            this.flowResolver = flowResolver;
        }
        public Memory<byte> Handle(string query)
        {
            return flowResolver.GetCitys().ToJson().ToBytes();
        }

        public void Free()
        {
        }
    }
}
