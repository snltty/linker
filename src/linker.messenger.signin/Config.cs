using linker.libs;
using linker.libs.extends;

namespace linker.messenger.signin
{
    public sealed partial class SignInClientGroupInfo
    {
        public SignInClientGroupInfo() { }

        // 【安全修复 P0】移除硬编码默认值
        public string Name { get; set; } = string.Empty;

        // 【安全修复 P0】统一使用随机凭证，不再使用硬编码值
        private string id = string.Empty;
        public string Id
        {
            get => id; set
            {
                id = value.SubStr(0, 36);
            }
        }

        // 【安全修复 P0】统一使用随机凭证，不再使用硬编码值
        private string passord = string.Empty;
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

        public string SecretKey { get; set; } = string.Empty;
        public string UserId { get; set; } = Guid.NewGuid().ToString();

        // 【安全修复 P0】DEBUG 和 Release 模式统一使用随机凭证
        // 不再在 DEBUG 模式下使用硬编码的 "snltty"
        public string SuperKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string SuperPassword { get; set; } = Guid.NewGuid().ToString().ToUpper();
    }

    public sealed class SignInConfigServerInfo
    {
        public int CleanDays { get; set; } = 7;

        public string SecretKey { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;
        public bool Anonymous { get; set; } = true;

        // 【安全修复 P0】移除硬编码凭证，统一使用随机生成
        public string SuperKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
        public string SuperPassword { get; set; } = Guid.NewGuid().ToString().ToUpper();


    }

    public sealed partial class SignInConfigSetNameInfo
    {
        public string Id { get; set; }
        public string NewName { get; set; }
    }
}
