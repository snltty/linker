using System.Collections.Concurrent;

namespace linker.messenger.sforward.server
{
    public sealed class SForwardServerConnectionTransfer
    {
        private readonly ConcurrentDictionary<string, IConnection>[] connections = [
            new ConcurrentDictionary<string, IConnection>()
            ,new ConcurrentDictionary<string, IConnection>()
        ];

        public SForwardServerConnectionTransfer()
        {

        }
        public List<IConnection> Get(ConnectionSideType type)
        {
            return connections[(byte)type].Values.Where(c => c.Connected).ToList();
        }

        public bool TryGet(ConnectionSideType type, string id, out IConnection connection)
        {
            return connections[(byte)type].TryGetValue(id, out connection);
        }
        public bool TryAdd(ConnectionSideType type, string id, IConnection connection)
        {
            if (connections[(byte)type].TryRemove(id, out IConnection _connection) && _connection.GetHashCode() != connection.GetHashCode())
            {
                _connection.Disponse();
            }

            return connections[(byte)type].TryAdd(id, connection);
        }
    }

    public enum ConnectionSideType : byte
    {
        Node = 0,
        Master = 1
    }
}
