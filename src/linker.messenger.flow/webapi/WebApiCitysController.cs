using linker.libs.extends;
using linker.libs.web;
using System.Threading.Tasks;
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
        public async Task<Memory<byte>> Handle(string query)
        {
            return await Task.FromResult(flowResolver.GetCitys().ToJson().ToBytes()).ConfigureAwait(false);
        }

        public void Free()
        {
        }
    }
}
