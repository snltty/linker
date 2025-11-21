using linker.libs;
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
            Helper.OnAppExit += Helper_OnAppExit;
        }

        private void Helper_OnAppExit(object sender, EventArgs e)
        {
            Disponse();
        }

        public string SignInHost { get; set; }

        /// <summary>
        /// 登入服务器的连接对象
        /// </summary>
        [JsonIgnore]
        public IConnection Connection { get; set; }
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool Connected => Connection != null && Connection.Connected;
        /// <summary>
        /// 服务器版本
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// 外网IP端口
        /// </summary>
        public IPEndPoint WanAddress { get; set; } = new IPEndPoint(IPAddress.Any, 0);

        public bool Super { get; set; }

        /// <summary>
        /// 登录之前
        /// </summary>
        [JsonIgnore]
        public Func<Task> OnSignInBrfore { get; set; } = async () => { await Task.CompletedTask; };
        public async Task PushSignInBefore()
        {
            await OnSignInBrfore?.Invoke();
        }


        private int signInTimes = 0;
        [JsonIgnore]
        public Action OnSignInSuccessBefore { get; set; } = () => { };
        /// <summary>
        /// 上线事件
        /// </summary>
        [JsonIgnore]
        public Action<int> OnSignInSuccess { get; set; } = (i) => { };
        /// <summary>
        /// 第一次上线
        /// </summary>
        [JsonIgnore]
        public Action OnSignInSuccessFirstTime { get; set; } = () => { };

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
