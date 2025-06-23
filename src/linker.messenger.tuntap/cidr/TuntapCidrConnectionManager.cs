using linker.libs;
using linker.tunnel.connection;
using System.Collections.Concurrent;

namespace linker.messenger.tuntap.cidr
{
    public sealed class TuntapCidrConnectionManager
    {
        private readonly ConcurrentDictionary<uint, ITunnelConnection> connections = new ConcurrentDictionary<uint, ITunnelConnection>();
        public ConcurrentDictionary<uint, ITunnelConnection> Connections => connections;

        public VersionManager Version { get; } = new VersionManager();


        public TuntapCidrConnectionManager() { }

        public void Add(uint ip, ITunnelConnection connection)
        {
            if (connection == null) return;
            connections.AddOrUpdate(ip, connection, (key, oldValue) => connection);
            Version.Increment();
        }
        public void Update(ITunnelConnection connection)
        {
            if (connection == null) return;
            List<uint> keys = connections.Where(c => c.Value.RemoteMachineId == connection.RemoteMachineId).Select(c => c.Key).ToList();
            foreach (uint ip in keys)
            {
                connections.AddOrUpdate(ip, connection, (a, b) => connection);
            };
            Version.Increment();
        }
        public void Remove(uint ip)
        {
            connections.TryRemove(ip, out _);
            Version.Increment();
        }
        public void Remove(string machineId)
        {
            foreach (var item in connections.Where(c => c.Value.RemoteMachineId == machineId).ToList())
            {
                connections.TryRemove(item.Key, out _);
            }
            Version.Increment();
        }
        public void RemoveNotMachine(uint ip, string machineId)
        {
            if (connections.TryGetValue(ip, out ITunnelConnection connection) && machineId != connection.RemoteMachineId)
            {
                connections.TryRemove(ip, out _);
            }
            Version.Increment();
        }
        public void RemoveNotMachine(uint network, uint maskValue, string machineId)
        {
            foreach (var item in connections.Where(c => (c.Key & maskValue) == network && c.Value.RemoteMachineId != machineId).ToList())
            {
                connections.TryRemove(item.Key, out _);
            }
            Version.Increment();
        }

        public bool TryGet(uint ip, out ITunnelConnection connection)
        {
            return connections.TryGetValue(ip, out connection);
        }

        public void Clear()
        {
            connections.Clear();
            Version.Increment();
        }
    }
}
