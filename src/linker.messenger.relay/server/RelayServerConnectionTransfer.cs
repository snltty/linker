using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.messenger.relay.server
{
    public sealed class RelayServerConnectionTransfer
    {
        private readonly ConcurrentDictionary<string, IConnection> connections = new();

        public bool TryGet(string id,out IConnection connection)
        {
            return connections.TryGetValue(id, out connection);
        }
    }
}
