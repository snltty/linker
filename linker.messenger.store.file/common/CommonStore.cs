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

        private readonly FileConfig fileConfig;
        public CommonStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
    }
}
