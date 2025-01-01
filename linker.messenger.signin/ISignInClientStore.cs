namespace linker.messenger.signin
{
    public interface ISignInClientStore
    {
        public SignInClientServerInfo Server { get; }
        public SignInClientGroupInfo Group { get; }
        public string Id { get; }
        public string Name { get; }

        public void SetName(string newName);
        public void SetGroups(SignInClientGroupInfo[] groups);
        public void SetGroupPassword(string password);
        public void SetServer(SignInClientServerInfo servers);
        public void SetSecretKey(string secretKey);
        public void SetId(string id);
    }

}
