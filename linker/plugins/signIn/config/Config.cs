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
