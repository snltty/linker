using linker.libs.extends;
using linker.libs.web;
using linker.messenger.signin;
using System.Threading.Tasks;
namespace linker.messenger.flow.webapi
{
    public sealed class WebApiOnlineController : IWebApiController
    {
        public string Path => "/flow/online.json";

        private readonly SignInServerCaching signCaching;
        private readonly FlowResolver flowResolver;
        public WebApiOnlineController(SignInServerCaching signCaching, FlowResolver flowResolver)
        {
            this.signCaching = signCaching;
            this.flowResolver = flowResolver;
        }
        public async Task<Memory<byte>> Handle(string query)
        {
            signCaching.GetOnline(out int all, out int online);
            return await Task.FromResult(new
            {
                CurrentServer = new
                {
                    Online7day = all,
                    Online = online,
                },
                AllServer = new
                {
                    Online7day = flowResolver.ReceiveBytes & 0xffffffff,
                    Online = flowResolver.ReceiveBytes >> 32,
                    Server = flowResolver.SendtBytes,
                }
            }.ToJson().ToBytes()).ConfigureAwait(false);
        }

        public void Free()
        {
        }
    }
}
