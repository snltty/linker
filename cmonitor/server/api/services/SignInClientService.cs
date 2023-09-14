using cmonitor.server.service.messengers.sign;

namespace cmonitor.server.api.services
{
    public sealed class SignInClientService : IClientService
    {
        private readonly SignCaching signCaching;
        private readonly Config config;
        public SignInClientService(SignCaching signCaching, Config config)
        {
            this.signCaching = signCaching;
            this.config = config;
        }
        public List<SignCacheInfo> List(ClientServiceParamsInfo param)
        {
            List<SignCacheInfo> caches = signCaching.Get();
            return caches;
        }
        public bool Del(ClientServiceParamsInfo param)
        {
            signCaching.Del(param.Content);

            return true;

        }
        public Config Config(ClientServiceParamsInfo param)
        {
            return config;
        }
    }
}
