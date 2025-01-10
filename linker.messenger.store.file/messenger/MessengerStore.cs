using linker.libs;
using System.Security.Cryptography.X509Certificates;

namespace linker.messenger.store.file.messenger
{
    public class MessengerStore : IMessengerStore
    {
        public ServerCertificateInfo SSL => fileConfig.Data.Common.SSL;
        public X509Certificate Certificate => certificate;

        private readonly FileConfig fileConfig;

        private X509Certificate certificate;
        public MessengerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;

            string path = Path.GetFullPath(SSL.File);
            if (File.Exists(path))
            {
                if (SSL.Password == "oeq9tw1o") SSL.Password = "snltty";

                certificate = new X509Certificate(path, SSL.Password, X509KeyStorageFlags.Exportable);
            }
            else
            {
                LoggerHelper.Instance.Error($"file {path} not found");
                Environment.Exit(0);
            }
        }
    }
}
