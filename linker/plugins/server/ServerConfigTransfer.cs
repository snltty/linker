using linker.config;

namespace linker.plugins.server
{
    public sealed class ServerConfigTransfer
    {
        public int Port => config.Data.Server.ServicePort;
        public ServerCertificateInfo SSL => config.Data.Server.SSL;

        private readonly FileConfig config;
        public ServerConfigTransfer(FileConfig config)
        {
            this.config = config;
        }
    }
}
