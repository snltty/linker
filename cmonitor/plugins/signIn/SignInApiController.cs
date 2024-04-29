using cmonitor.server.api;
using cmonitor.config;
using cmonitor.plugins.signin.messenger;
using common.libs.api;

namespace cmonitor.plugins.signin
{
    public sealed class SignInApiController : IApiServerController
    {
        private readonly SignCaching signCaching;
        private readonly Config config;
        public SignInApiController(SignCaching signCaching, Config config)
        {
            this.signCaching = signCaching;
            this.config = config;
        }
        public List<SignCacheInfo> List(ApiControllerParamsInfo param)
        {
            List<SignCacheInfo> caches = signCaching.Get(param.Content);
            return caches;
        }
        public bool Del(ApiControllerParamsInfo param)
        {
            signCaching.Del(param.Content);

            return true;

        }
        public Config Config(ApiControllerParamsInfo param)
        {
            return config;
        }
    }
}
