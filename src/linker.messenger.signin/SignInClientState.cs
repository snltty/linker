using System.Net;
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
            if (OperatingSystem.IsAndroid() == false)
            {
                AppDomain.CurrentDomain.ProcessExit += (s, e) => Disponse();
                Console.CancelKeyPress += (s, e) => Disponse();
            }
        }

        /// <summary>
        /// 登入服务器的连接对象
        /// </summary>
        [JsonIgnore]
        public IConnection Connection { get; set; }
        public bool Connected => Connection != null && Connection.Connected;

        public string Version { get; set; }
        public IPEndPoint WanAddress { get; set; } = new IPEndPoint(IPAddress.Any, 0);


        /// <summary>
        /// 登录之前
        /// </summary>
        [JsonIgnore]
        public Func<Task> OnSignInBrfore { get; set; }
        public async Task PushSignInBefore()
        {
            await OnSignInBrfore?.Invoke();
        }


        private int signInTimes = 0;
        [JsonIgnore]
        public Action OnSignInSuccessBefore { get; set; }
        /// <summary>
        /// 上线事件
        /// </summary>
        [JsonIgnore]
        public Action<int> OnSignInSuccess { get; set; }
        /// <summary>
        /// 第一次上线
        /// </summary>
        [JsonIgnore]
        public Action OnSignInSuccessFirstTime { get; set; }

        /// <summary>
        /// 发布上线事件
        /// </summary>
        public void PushSignInSuccessBefore()
        {
            OnSignInSuccessBefore?.Invoke();
        }
        /// <summary>
        /// 发布上线事件
        /// </summary>
        public void PushSignInSuccess()
        {
            if (signInTimes == 0)
            {
                OnSignInSuccessFirstTime?.Invoke();
            }
            OnSignInSuccess?.Invoke(signInTimes);
            signInTimes++;
        }

        public void Disponse()
        {
            Connection?.Disponse();
        }
    }
}
