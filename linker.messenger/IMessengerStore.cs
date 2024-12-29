using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
