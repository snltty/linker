using linker.libs;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace linker.messenger.store.file.messenger
{
    public class MessengerStore : IMessengerStore
    {
        public X509Certificate Certificate => certificate;

        private readonly FileConfig fileConfig;

        private X509Certificate2 certificate;
        public MessengerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;

            using Stream streamPublic = Assembly.GetExecutingAssembly().GetManifestResourceStream($"linker.messenger.store.file.publickey.pem");
            using Stream streamPrivate = Assembly.GetExecutingAssembly().GetManifestResourceStream($"linker.messenger.store.file.privatekey.pem");

            using StreamReader readerPublic = new StreamReader(streamPublic);
            using StreamReader readerPrivate = new StreamReader(streamPrivate);

            RSA rsaPrivateKey = RSA.Create();
            rsaPrivateKey.ImportFromPem(readerPrivate.ReadToEnd());

            using X509Certificate2 publicCert = X509Certificate2.CreateFromPem(readerPublic.ReadToEnd());
            certificate = publicCert.CopyWithPrivateKey(rsaPrivateKey);

            if (OperatingSystem.IsAndroid() == false)
            {
                //不导出不支持windows什么的
                byte[] pfxBytes = certificate.Export(X509ContentType.Pfx, Helper.GlobalString);
                certificate.Dispose();
#pragma warning disable SYSLIB0057 // 类型或成员已过时
                certificate = new X509Certificate2(pfxBytes, Helper.GlobalString);
#pragma warning restore SYSLIB0057 // 类型或成员已过时
            }
        }
    }
}
