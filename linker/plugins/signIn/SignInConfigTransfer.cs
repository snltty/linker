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
