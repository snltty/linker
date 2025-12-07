using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.sforward.server
{
    public interface ISForwardServerCahing
    {
        public bool TryAdd(string domain, string machineId, string masterNodeId);
        public bool TryAdd(int port, string machineId, string masterNodeId);

        public bool TryGet(string domain, out string machineId, out string masterNodeId);
        public bool TryGet(int port, out string machineId, out string masterNodeId);
        public bool TryGet(List<string> ids, out List<string> domains, out List<int> ports);

        public bool TryRemove(string domain, string operMachineId, string masterNodeId, out string machineId);
        public bool TryRemove(string domain, string masterNodeId, out string machineId);
        public bool TryRemove(int port, string operMachineId, string masterNodeId, out string machineId);
        public bool TryRemove(int port, string masterNodeId, out string machineId);

        /// <summary>
        /// 信标服务器id，客户端id列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> GetMachineIds();
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

        public bool TryAdd(string domain, string machineId, string masterNodeId)
        {
            if (serverDoamins.TryGetValue(domain, out CacheInfo cache) && machineId == cache.MachineId)
            {
                return true;
            }
            if (signCaching.GetOnline(machineId) == false)
            {
                serverDoamins.TryRemove(domain, out _);
            }

            return serverDoamins.TryAdd(domain, new CacheInfo { MachineId = machineId, MasterNodeId = masterNodeId });
        }

        public bool TryAdd(int port, string machineId, string masterNodeId)
        {
            if (serverPorts.TryGetValue(port, out CacheInfo cache) && machineId == cache.MachineId)
            {
                return true;
            }
            if (signCaching.GetOnline(machineId) == false)
            {
                serverPorts.TryRemove(port, out _);
            }

            return serverPorts.TryAdd(port, new CacheInfo { MachineId = machineId, MasterNodeId = masterNodeId });
        }

        public bool TryGet(string domain, out string machineId, out string masterNodeId)
        {
            machineId = string.Empty;
            masterNodeId = string.Empty;
            if (serverDoamins.TryGetValue(domain, out CacheInfo cache))
            {
                machineId = cache.MachineId;
                masterNodeId = cache.MasterNodeId;
                return true;
            }
            return false;
        }

        public bool TryGet(int port, out string machineId, out string masterNodeId)
        {
            machineId = string.Empty;
            masterNodeId = string.Empty;
            if (serverPorts.TryGetValue(port, out CacheInfo cache))
            {
                machineId = cache.MachineId;
                masterNodeId = cache.MasterNodeId;
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

        public bool TryRemove(string domain, string operMachineId, string masterNodeId, out string machineId)
        {
            machineId = string.Empty;
            if (serverDoamins.TryGetValue(domain, out CacheInfo cache) && cache.MachineId == operMachineId && cache.MasterNodeId == masterNodeId)
            {
                if (serverDoamins.TryRemove(domain, out CacheInfo cache1))
                {
                    machineId = cache1.MachineId;
                    return true;
                }
            }
            return false;
        }
        public bool TryRemove(string domain, string masterNodeId, out string machineId)
        {
            machineId = string.Empty;
            if (serverDoamins.TryGetValue(domain, out CacheInfo cache) && cache.MasterNodeId == masterNodeId)
            {
                if (serverDoamins.TryRemove(domain, out _))
                {
                    machineId = cache.MachineId;
                    return true;
                }
            }

            return false;
        }

        public bool TryRemove(int port, string operMachineId, string masterNodeId, out string machineId)
        {
            machineId = string.Empty;
            if (serverPorts.TryGetValue(port, out CacheInfo cache) && cache.MachineId == operMachineId && cache.MasterNodeId == masterNodeId)
            {
                if (serverPorts.TryRemove(port, out CacheInfo cache1))
                {
                    machineId = cache1.MachineId;
                    return true;
                }
            }
            return false;
        }
        public bool TryRemove(int port, string masterNodeId, out string machineId)
        {
            machineId = string.Empty;
            if (serverPorts.TryGetValue(port, out CacheInfo cache) && cache.MasterNodeId == masterNodeId)
            {
                if (serverPorts.TryRemove(port, out _))
                {
                    machineId = cache.MachineId;
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, List<string>> GetMachineIds()
        {
            return serverDoamins.Values.Select(c => (c.MasterNodeId, c.MachineId))
                .Union(serverPorts.Values.Select(c => (c.MasterNodeId, c.MachineId))).Distinct()
                .GroupBy(c => c.MasterNodeId).ToDictionary(c => c.Key, d => d.Select(c => c.MachineId).ToList());
        }

        sealed class CacheInfo
        {
            public string MachineId { get; set; }
            public string MasterNodeId { get; set; }
            public long LastTime { get; set; } = Environment.TickCount64;
        }
    }
}
