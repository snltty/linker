namespace linker.messenger.store.file.common
{
    public sealed class CommonStore : ICommonStore
    {
        public CommonModes Modes
        {
            get
            {
                CommonModes modes = 0;
                if (fileConfig.Data.Common.Modes.Contains("client"))
                {
                    modes |= CommonModes.Client;
                }
                if (fileConfig.Data.Common.Modes.Contains("server"))
                {
                    modes |= CommonModes.Server;
                }
                return modes;
            }
        }
        public bool Installed => fileConfig.Data.Common.Install;

        private readonly FileConfig fileConfig;
        public CommonStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        public void SetModes(CommonModes modes)
        {
            List<string> modesStr = new List<string>();
            if((modes & CommonModes.Client) == CommonModes.Client)
            {
                modesStr.Add("client");
            }
            if ((modes & CommonModes.Server) == CommonModes.Server)
            {
                modesStr.Add("server");
            }
            fileConfig.Data.Common.Modes = modesStr.ToArray();
        }

        public void SetInstalled(bool installed)
        {
            fileConfig.Data.Common.Install = installed;
        }

        public void Confirm()
        {
            fileConfig.Data.Update();
        }
    }
}
