using cmonitor.api;
using cmonitor.config;
using cmonitor.plugins.signIn.messenger;

namespace cmonitor.plugins.signIn
{
    public sealed class SignInApiController : IApiController
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
            List<SignCacheInfo> caches = signCaching.Get();
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
