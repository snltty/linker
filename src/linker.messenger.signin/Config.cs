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
        private string id = string.Empty;
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
        private string passord = string.Empty;
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
        public string SecretKey { get; set; } = string.Empty;
        public string UserId { get; set; } = Guid.NewGuid().ToString();
        
    }

    public sealed class SignInConfigServerInfo
    {
        public string SecretKey { get; set; } = string.Empty;
        public int CleanDays { get; set; } = 7;
    }

    public sealed partial class SignInConfigSetNameInfo
    {
        public string Id { get; set; }
        public string NewName { get; set; }
    }
}
