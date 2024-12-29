using linker.config;
using linker.libs;
using linker.messenger.api;

namespace linker.messenger.store.file.api
{
    public sealed class ApiStore : IApiStore
    {
        public ApiClientInfo Info => fileConfig.Data.Client.CApi;

        private readonly FileConfig fileConfig;
        public ApiStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

       
    }
}
