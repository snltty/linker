using linker.client.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace linker.plugins.tunnel.excludeip
{
    public interface IExcludeIP
    {
        public ExcludeIPItem[] Get();
    }
}
