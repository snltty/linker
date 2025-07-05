using linker.libs;
using linker.messenger.signin;
using linker.messenger.sync;

namespace linker.messenger.store.file.signIn
{
    public sealed class SignInSyncServer : ISync
    {
        public string Name => "SignInServer";

        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        public SignInSyncServer(ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize((signInClientStore.Server.Host, signInClientStore.Server.Host1));
        }

        public void SetData(Memory<byte> data)
        {
            ValueTuple<string, string> serverInfo = serializer.Deserialize<ValueTuple<string, string>>(data.Span);
            signInClientStore.SetHost(serverInfo.Item1, serverInfo.Item2);
        }
    }
    public sealed class SignInServerSyncInfo
    {
        public string Host { get; set; } = string.Empty;
        public string Host1 { get; set; } = string.Empty;
    }

    public sealed class SignInSyncSecretKey : ISync
    {
        public string Name => "SignInSuperKey";

        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        public SignInSyncSecretKey(ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(new KeyValuePair<string, string>(signInClientStore.Server.SuperKey, signInClientStore.Server.SuperPassword));
        }

        public void SetData(Memory<byte> data)
        {
            KeyValuePair<string, string> info = serializer.Deserialize<KeyValuePair<string, string>>(data.Span);
            signInClientStore.SetSuper(info.Key, info.Value);
        }
    }
    public sealed class SignInSyncUserId : ISync
    {
        public string Name => "SignInUserId";

        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        public SignInSyncUserId(ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(signInClientStore.Server.UserId);
        }

        public void SetData(Memory<byte> data)
        {
            signInClientStore.SetUserId(serializer.Deserialize<string>(data.Span));
        }
    }
    public sealed class SignInSyncGroupSecretKey : ISync
    {
        public string Name => "GroupSecretKey";

        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        public SignInSyncGroupSecretKey(ISignInClientStore signInClientStore, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
        }
        public Memory<byte> GetData()
        {
            return serializer.Serialize(signInClientStore.Group.Password);
        }

        public void SetData(Memory<byte> data)
        {
            signInClientStore.SetGroupPassword(serializer.Deserialize<string>(data.Span));
        }
    }
}
