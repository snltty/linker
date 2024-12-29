namespace linker.messenger.store.file.messenger
{
    public class MessengerStore : IMessengerStore
    {
        public ServerCertificateInfo SSL => fileConfig.Data.Server.SSL;

        private readonly FileConfig fileConfig;
        public MessengerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }
    }
}
