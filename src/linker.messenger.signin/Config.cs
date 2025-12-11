using linker.libs;
using linker.libs.extends;

namespace linker.messenger.signin
{
    public sealed partial class SignInClientGroupInfo
    {
        public SignInClientGroupInfo() { }

        public string Name { get; set; } = Helper.GlobalString;

#if DEBUG
        private string id = Helper.GlobalString;
#else
        private string id = Guid.NewGuid().ToString();
#endif
        public string Id
        {
            get => id; set
            {
                id = value.SubStr(0, 36);
            }
        }

#if DEBUG
        private string passord = Helper.GlobalString;
#else
        private string passord = Guid.NewGuid().ToString();
#endif
        public string Password
        {
            get => passord; set
            {
                passord = value.SubStr(0, 36);
            }
        }
    }

    public sealed partial class SignInClientServerInfo
    {
        public SignInClientServerInfo() { }
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Host1 { get; set; } = string.Empty;
        public string[] Hosts { get; set; } = [];

        public string UserId { get; set; } = Guid.NewGuid().ToString();

#if DEBUG
        public string SuperKey { get; set; } = Helper.GlobalString;
        public string SuperPassword { get; set; } = Helper.GlobalString;
#else
        public string SuperKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string SuperPassword { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
    }

    public sealed class SignInConfigServerInfo
    {
        public int CleanDays { get; set; } = 7;

        public bool Enabled { get; set; } = true;
        public bool Anonymous { get; set; } = true;

#if DEBUG
        public string SuperKey { get; set; } = Helper.GlobalString;
        public string SuperPassword { get; set; } = Helper.GlobalString;
#else
        public string SuperKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string SuperPassword { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif


    }

    public sealed partial class SignInConfigSetNameInfo
    {
        public string Id { get; set; }
        public string NewName { get; set; }
    }
}
