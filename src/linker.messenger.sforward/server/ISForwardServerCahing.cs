using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.sforward.server
{
    public interface ISForwardServerCahing
    {
        public bool TryAdd(string domain, string machineId);
        public bool TryAdd(int port, string machineId);

        public bool TryGet(string domain, out string machineId);
        public bool TryGet(int port, out string machineId);
        public bool TryGet(List<string> ids, out List<string> domains, out List<int> ports);

        public bool TryRemove(string domain, string operMachineId, out string machineId);
        public bool TryRemove(string domain, out string machineId);
        public bool TryRemove(int port, string operMachineId, out string machineId);
        public bool TryRemove(int port, out string machineId);

        public List<string> GetMachineIds();
    }

    /// <summary>
    /// 服务器穿透缓存，用于识别不同的客户端
    /// </summary>
    public sealed class SForwardServerCahing : ISForwardServerCahing
    {
        private readonly ConcurrentDictionary<string, CacheInfo> serverDoamins = new();
        private readonly ConcurrentDictionary<int, CacheInfo> serverPorts = new();

        private readonly SignInServerCaching signCaching;
        public SForwardServerCahing(SignInServerCaching signCaching)
        {
            this.signCaching = signCaching;
        }

        public bool TryAdd(string domain, string machineId)
        {
            if (serverDoamins.TryGetValue(domain, out CacheInfo cache) && machineId == cache.MachineId)
            {
                return true;
            }
            if (signCaching.GetOnline(machineId) == false)
            {
                serverDoamins.TryRemove(domain, out _);
            }

            return serverDoamins.TryAdd(domain, new CacheInfo { MachineId = machineId });
        }

        public bool TryAdd(int port, string machineId)
        {
            if (serverPorts.TryGetValue(port, out CacheInfo cache) && machineId == cache.MachineId)
            {
                return true;
            }
            if (signCaching.GetOnline(machineId) == false)
            {
                serverPorts.TryRemove(port, out _);
            }

            return serverPorts.TryAdd(port, new CacheInfo { MachineId = machineId });
        }

        public bool TryGet(string domain, out string machineId)
        {
            machineId = string.Empty;
            if (serverDoamins.TryGetValue(domain, out CacheInfo cache))
            {
                machineId = cache.MachineId;
                return true;
            }
            return false;
        }

        public bool TryGet(int port, out string machineId)
        {
            machineId = string.Empty;
            if (serverPorts.TryGetValue(port, out CacheInfo cache))
            {
                machineId = cache.MachineId;
                return true;
            }
            return false;
        }
        public bool TryGet(List<string> ids, out List<string> domains, out List<int> ports)
        {
            domains = serverDoamins.Where(c => ids.Contains(c.Value.MachineId)).Select(c => c.Key).ToList();
            ports = serverPorts.Where(c => ids.Contains(c.Value.MachineId)).Select(c => c.Key).ToList();
            return domains.Count > 0 || ports.Count > 0;
        }

        public bool TryRemove(string domain, string operMachineId, out string machineId)
        {
            machineId = string.Empty;
            if (serverDoamins.TryGetValue(domain, out CacheInfo cache) && cache.MachineId == operMachineId)
            {
                if (serverDoamins.TryRemove(domain, out CacheInfo cache1))
                {
                    machineId = cache1.MachineId;
                    return true;
                }
            }
            return false;
        }
        public bool TryRemove(string domain, out string machineId)
        {
            machineId = string.Empty;
            if (serverDoamins.TryRemove(domain, out CacheInfo cache))
            {
                machineId = cache.MachineId;
                return true;
            }
            return false;
        }

        public bool TryRemove(int port, string operMachineId, out string machineId)
        {
            machineId = string.Empty;
            if (serverPorts.TryGetValue(port, out CacheInfo cache) && cache.MachineId == operMachineId)
            {
                if (serverPorts.TryRemove(port, out CacheInfo cache1))
                {
                    machineId = cache1.MachineId;
                    return true;
                }
            }
            return false;
        }
        public bool TryRemove(int port, out string machineId)
        {
            machineId = string.Empty;
            if (serverPorts.TryRemove(port, out CacheInfo cache1))
            {
                machineId = cache1.MachineId;
                return true;
            }
            return false;
        }

        public List<string> GetMachineIds()
        {
            return serverDoamins.Values.Select(c => c.MachineId).Union(serverPorts.Values.Select(c => c.MachineId)).Distinct().ToList();
        }

        sealed class CacheInfo
        {
            public string MachineId { get; set; }
            public long LastTime { get; set; } = Environment.TickCount64;
        }
    }
}
