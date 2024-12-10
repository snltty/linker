using linker.config;

namespace linker.plugins.client
{
    public sealed class ClientConfigTransfer
    {
        public ClientServerInfo Server => config.Data.Client.Servers[0];
        public ClientGroupInfo Group => config.Data.Client.Groups[0];
        public ClientCertificateInfo SSL => config.Data.Client.SSL;
        
        public string Id => config.Data.Client.Id;
        public string Name => config.Data.Client.Name;

        private readonly FileConfig config;
        public ClientConfigTransfer(FileConfig config)
        {
            this.config = config;
        }

        public void SetName(string newName)
        {
            config.Data.Client.Name = newName;
            config.Data.Update();
        }
        public void SetGroup(ClientGroupInfo[] groups)
        {
            config.Data.Client.Groups = groups.DistinctBy(c => c.Name).ToArray();
            config.Data.Update();
        }
        public void SetGroupPassword(string password)
        {
            Group.Password = password;
            config.Data.Update();
        }
        public void SetServer(ClientServerInfo[] servers)
        {
            config.Data.Client.Servers = servers;
            config.Data.Update();
        }

        public void SetId(string id)
        {
            config.Data.Client.Id = id;
            config.Data.Update();
        }
    }
}
