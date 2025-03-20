using linker.libs;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace linker.messenger.store.file.messenger
{
    public class MessengerStore : IMessengerStore
    {
        public ServerCertificateInfo SSL => fileConfig.Data.Common.SSL;
        public X509Certificate Certificate => certificate;

        private readonly FileConfig fileConfig;

        private X509Certificate2 certificate;
        public MessengerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
            string path = Path.GetFullPath(Path.Join(Helper.currentDirectory, SSL.File));
            if (SSL.Password == "oeq9tw1o")
            {
                SSL.Password = Helper.GlobalString;
                fileConfig.Data.Update();
            }
            if (File.Exists(path))
            {
                certificate = new X509Certificate2(path, SSL.Password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.DefaultKeySet);
            }
            else
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"linker.messenger.store.file.{Helper.GlobalString}.pfx");
                using MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                certificate = new X509Certificate2(memoryStream.ToArray(), Helper.GlobalString, X509KeyStorageFlags.Exportable);
            }

        }
    }
}
