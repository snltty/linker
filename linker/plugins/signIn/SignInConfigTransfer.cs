using linker.config;

namespace linker.plugins.signIn
{
    public sealed class SignInConfigTransfer
    {
        public string SecretKey => config.Data.Server.SignIn.SecretKey;

        private readonly FileConfig config;
        public SignInConfigTransfer(FileConfig config)
        {
            this.config = config;
        }
    }
}

namespace linker.config
{
    public partial class ConfigServerInfo
    {
        /// <summary>
        /// 登入
        /// </summary>
        public SignInConfigServerInfo SignIn { get; set; } = new SignInConfigServerInfo();
    }

    public sealed class SignInConfigServerInfo
    {
        public string SecretKey { get; set; } = string.Empty;
    }
}
