using linker.libs;
using System.Security.Cryptography.X509Certificates;

namespace linker.messenger.store.file.messenger
{
    public class MessengerStore : IMessengerStore
    {
        public ServerCertificateInfo SSL => fileConfig.Data.Common.SSL;
        public X509Certificate2 Certificate => certificate;

        private readonly FileConfig fileConfig;

        private X509Certificate2 certificate;
        public MessengerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;

            string path = Path.GetFullPath(SSL.File);
            if (File.Exists(path))
            {
                certificate = new X509Certificate2(path, SSL.Password, X509KeyStorageFlags.Exportable);
            }
            else
            {
                LoggerHelper.Instance.Error($"file {path} not found");
                Environment.Exit(0);
            }
        }
    }
}
