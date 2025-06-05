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

        public bool Set(ApiClientInfo Info)
        {
            fileConfig.Data.Client.CApi = Info;
            return true;
        }

        public bool Confirm()
        {
            fileConfig.Data.Update();
            return true;
        }

        public bool SetApiPassword(string password)
        {
            fileConfig.Data.Client.CApi.ApiPassword = password;
            return true;
        }

        public bool SetWebPort(int port)
        {
            fileConfig.Data.Client.CApi.WebPort= port;
            return true;
        }

        public bool SetWebRoot(string rootPath)
        {
            fileConfig.Data.Client.CApi.WebRoot = rootPath;
            return true;
        }
    }
}
