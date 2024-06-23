namespace link.plugins.sforward.config
{
    public interface ISForwardServerCahing
    {
        public bool TryAdd(string domain, string machineId);
        public bool TryAdd(int port, string machineId);

        public bool TryGet(string domain, out string machineId);
        public bool TryGet(int port, out string machineId);

        public bool TryRemove(string domain, string operMachineId, out string machineId);
        public bool TryRemove(int port, string operMachineId, out string machineId);
        public bool TryRemove(string machineId, out List<int> ports);
    }
}
