﻿using System.Net;
using System.Text.Json.Serialization;
namespace linker.messenger.signin
{
    /// <summary>
    /// 登入对象
    /// </summary>
    public sealed class SignInClientState
    {

        public SignInClientState()
        {
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Disponse();
            Console.CancelKeyPress += (s, e) => Disponse();
        }

        /// <summary>
        /// 登入服务器的连接对象
        /// </summary>
        [JsonIgnore]
        public IConnection Connection { get; set; }

        [JsonIgnore]
        public bool connecting = false;
        public bool Connecting => connecting;
        public bool Connected => Connection != null && Connection.Connected;

        public string Version { get; set; }

        public IPEndPoint WanAddress { get; set; } = new IPEndPoint(IPAddress.Any, 0);
        public IPEndPoint LanAddress => Connection?.LocalAddress ?? new IPEndPoint(IPAddress.Any, 0);


        private int networdkEnabledTimes = 0;
        [JsonIgnore]
        public Action NetworkEnabledHandleBefore { get; set; }
        /// <summary>
        /// 上线事件
        /// </summary>
        [JsonIgnore]
        public Action<int> NetworkEnabledHandle { get; set; }
        /// <summary>
        /// 第一次上线
        /// </summary>
        [JsonIgnore]
        public Action NetworkFirstEnabledHandle { get; set; }

        public void PushNetworkEnabledBefore()
        {
            if (networdkEnabledTimes == 0)
            {
                NetworkFirstEnabledHandle?.Invoke();
            }
            NetworkEnabledHandleBefore?.Invoke();
        }
        /// <summary>
        /// 发布上线事件
        /// </summary>
        public void PushNetworkEnabled()
        {
            if (networdkEnabledTimes == 0)
            {
                NetworkFirstEnabledHandle?.Invoke();
            }
            NetworkEnabledHandle?.Invoke(networdkEnabledTimes);
            networdkEnabledTimes++;
        }

        public void Disponse()
        {
            Connection?.Disponse();
        }
    }
}
