using linker.libs.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.messenger.store.file
{
    public sealed partial class ConfigCommonInfo
    {
        public ServerCertificateInfo SSL { get; set; } = new ServerCertificateInfo();
    }
}
