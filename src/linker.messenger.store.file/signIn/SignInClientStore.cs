using linker.messenger.signin;

namespace linker.messenger.store.file.signIn
{
    public sealed class SignInClientStore : ISignInClientStore
    {
        public SignInClientServerInfo Server => config.Data.Client.Servers[0];
        public SignInClientGroupInfo Group => config.Data.Client.Groups[0];

        public string Id => config.Data.Client.Id;
        public string Name => config.Data.Client.Name;

        private readonly FileConfig config;
        public SignInClientStore(FileConfig config)
        {
            this.config = config;
        }

        public void SetName(string newName)
        {
            config.Data.Client.Name = newName;
            config.Data.Update();
        }
        public void SetGroups(SignInClientGroupInfo[] groups)
        {
            if (groups == null || groups.Length == 0) return;
            config.Data.Client.Groups = groups;
            config.Data.Update();
        }
        public void SetGroupPassword(string password)
        {
            Group.Password = password;
            config.Data.Update();
        }
        public void SetServer(SignInClientServerInfo server)
        {
            config.Data.Client.Servers = [server];
            config.Data.Update();
        }

        public void SetSecretKey(string secretKey)
        {
            Server.SecretKey = secretKey;
            config.Data.Update();
        }
        public void SetHost(string host)
        {
            Server.Host = host;
            config.Data.Update();
        }

        public void SetId(string id)
        {
            config.Data.Client.Id = id;
            config.Data.Update();
        }

        public bool Confirm()
        {
            config.Data.Update();
            return true;
        }
    }
}
