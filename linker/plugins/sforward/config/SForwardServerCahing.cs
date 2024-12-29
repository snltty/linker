using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.plugins.sforward.config
{
    /// <summary>
    /// 服务器穿透缓存，用于识别不同的客户端
    /// </summary>
    public sealed class SForwardServerCahing : ISForwardServerCahing
    {
        private ConcurrentDictionary<string, string> serverDoamins = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<int, string> serverPorts = new ConcurrentDictionary<int, string>();

        private readonly SignInServerCaching signCaching;
        public SForwardServerCahing(SignInServerCaching signCaching)
        {
            this.signCaching = signCaching;
        }

        public bool TryAdd(string domain, string machineId)
        {
            if (serverDoamins.TryGetValue(domain, out string _machineId) && machineId == _machineId)
            {
                return true;
            }
            if (signCaching.GetOnline(machineId) == false)
            {
                serverDoamins.TryRemove(domain, out _);
            }

            return serverDoamins.TryAdd(domain, machineId);
        }

        public bool TryAdd(int port, string machineId)
        {
            if (serverPorts.TryGetValue(port, out string _machineId) && machineId == _machineId)
            {
                return true;
            }
            if (signCaching.GetOnline(machineId) == false)
            {
                serverPorts.TryRemove(port, out _);
            }

            return serverPorts.TryAdd(port, machineId);
        }

        public bool TryGet(string domain, out string machineId)
        {
            return serverDoamins.TryGetValue(domain, out machineId);
        }

        public bool TryGet(int port, out string machineId)
        {
            return serverPorts.TryGetValue(port, out machineId);
        }

        public bool TryRemove(string domain, string operMachineId, out string machineId)
        {
            if (serverDoamins.TryGetValue(domain, out machineId) && machineId == operMachineId)
            {
                return serverDoamins.TryRemove(domain, out machineId);
            }
            return false;
        }

        public bool TryRemove(int port, string operMachineId, out string machineId)
        {
            if (serverPorts.TryGetValue(port, out machineId) && machineId == operMachineId)
            {
                return serverPorts.TryRemove(port, out machineId);
            }
            return false;
        }

        public bool TryRemove(string machineId, out List<int> ports)
        {
            var domains = serverDoamins.Where(c => c.Value == machineId).Select(c => c.Key).ToList();
            ports = serverPorts.Where(c => c.Value == machineId).Select(c => c.Key).ToList();

            foreach (var item in domains)
            {
                serverDoamins.TryRemove(item, out _);
            }
            foreach (var item in ports)
            {
                serverPorts.TryRemove(item, out _);
            }

            return true;
        }
    }

}
