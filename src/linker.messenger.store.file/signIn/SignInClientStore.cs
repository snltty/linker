using linker.messenger.signin;

namespace linker.messenger.store.file.signIn
{
    public sealed class SignInClientStore : ISignInClientStore
    {
        public SignInClientServerInfo Server => config.Data.Client.Servers[0];
        public SignInClientGroupInfo Group => config.Data.Client.Groups[0];
        public SignInClientGroupInfo[] Groups => config.Data.Client.Groups;

        public string Id => config.Data.Client.Id;
        public string Name => config.Data.Client.Name;

        public string[] Hosts => Server.Hosts;

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

        public void SetSuper(string key, string password)
        {
            Server.SuperKey = key;
            Server.SuperPassword = password;
            config.Data.Update();
        }
        public void SetUserId(string userid)
        {
            Server.UserId = userid;
            config.Data.Update();
        }
        public void SetHost(string host, string host1)
        {
            Server.Host = host;
            Server.Host1 = host1;
            config.Data.Update();
        }
        public void SetHosts(string[] hosts)
        {
            Server.Hosts = hosts;
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
