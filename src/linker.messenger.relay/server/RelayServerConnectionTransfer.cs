using System.Collections.Concurrent;

namespace linker.messenger.relay.server
{
    public sealed class RelayServerConnectionTransfer
    {
        private readonly ConcurrentDictionary<string, IConnection> connections = new();

        public RelayServerConnectionTransfer()
        {

        }
        public List<IConnection> Get()
        {
            return connections.Values.Where(c => c.Connected).ToList();
        }

        public bool TryGet(string id, out IConnection connection)
        {
            return connections.TryGetValue(id, out connection);
        }
        public bool TryAdd(string id, IConnection connection)
        {
            if (connections.TryRemove(id, out IConnection _connection) && _connection.GetHashCode() != connection.GetHashCode())
            {
                _connection.Disponse();
            }

            return connections.TryAdd(id, connection);
        }
    }
}
