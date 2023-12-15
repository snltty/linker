using cmonitor.service;

namespace cmonitor.client
{
    public sealed class ClientSignInState
    {
        public IConnection Connection { get; set; }
        public bool Connected => Connection != null && Connection.Connected;

        private int networdkEnabledTimes = 0;
        public Action<int> NetworkEnabledHandle { get; set; }
        public bool NetworkEnabled => Connected;
        public void PushNetworkEnabled()
        {
            NetworkEnabledHandle?.Invoke(networdkEnabledTimes);
            networdkEnabledTimes++;
        }
    }
}
