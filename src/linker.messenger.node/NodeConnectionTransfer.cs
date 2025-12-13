using System.Collections.Concurrent;

namespace linker.messenger.node
{
    public class NodeConnectionTransfer
    {
        private readonly ConcurrentDictionary<string, ConnectionInfo>[] connections = [
            new ConcurrentDictionary<string, ConnectionInfo>()
            ,new ConcurrentDictionary<string, ConnectionInfo>()
        ];

        public NodeConnectionTransfer()
        {

        }
        public List<ConnectionInfo> Get(ConnectionSideType type)
        {
            return connections[(byte)type].Values.Where(c => c.Connection.Connected).ToList();
        }

        public bool TryGet(ConnectionSideType type, string id, out ConnectionInfo connection)
        {
            return connections[(byte)type].TryGetValue(id, out connection);
        }
        public bool TryAdd(ConnectionSideType type, string id, ConnectionInfo connection)
        {
            if (connections[(byte)type].TryRemove(id, out ConnectionInfo _connection) && _connection.Connection.GetHashCode() != connection.Connection.GetHashCode())
            {
                _connection.Connection.Disponse();
            }

            return connections[(byte)type].TryAdd(id, connection);
        }
    }

    public sealed class ConnectionInfo
    {
        public IConnection Connection { get; set; }
        public bool Manageable { get; init; }
    }

    public enum ConnectionSideType : byte
    {
        Node = 0,
        Master = 1
    }
}
