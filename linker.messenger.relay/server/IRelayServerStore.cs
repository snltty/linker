using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.messenger.relay.server
{
    public interface IRelayServerStore
    {
        public string SecretKey { get; }
    }
}
