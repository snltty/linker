using System.Security.Cryptography.X509Certificates;

namespace linker.messenger
{
    public sealed partial class ServerCertificateInfo
    {
        public ServerCertificateInfo() { }
        public string File { get; set; } = "./snltty.pfx";
        public string Password { get; set; } = "oeq9tw1o";
    }
    public interface IMessengerStore
    {
        public ServerCertificateInfo SSL { get; }

        public X509Certificate2 Certificate { get; }
    }
}
