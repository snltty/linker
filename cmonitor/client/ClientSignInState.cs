using cmonitor.server;
using System.Text.Json.Serialization;

namespace cmonitor.client
{
    public sealed class ClientSignInState
    {
        [JsonIgnore]
        public IConnection Connection { get; set; }

        public bool Connecting { get; set; }
        public bool Connected => Connection != null && Connection.Connected;

        private int networdkEnabledTimes = 0;
        [JsonIgnore]
        public Action NetworkDisabledHandle { get; set; }
        [JsonIgnore]
        public Action<int> NetworkEnabledHandle { get; set; }
        [JsonIgnore]
        public Action NetworkFirstEnabledHandle { get; set; }
        public void PushNetworkEnabled()
        {
            if (networdkEnabledTimes == 0)
            {
                NetworkFirstEnabledHandle?.Invoke();
            }
            NetworkEnabledHandle?.Invoke(networdkEnabledTimes);
            networdkEnabledTimes++;
        }
        public void PushNetworkDisabled()
        {
            NetworkDisabledHandle?.Invoke();
        }
    }
}
