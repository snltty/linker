using linker.libs;

namespace linker.messenger.wlist
{
    public interface IWhiteListServerStore
    {
        public bool ValidateSecretKey(string secretKey);
        public Task<WhiteListPageResultInfo> Page(WhiteListPageRequestInfo request);
        public Task<bool> Add(WhiteListInfo info);
        public Task<bool> Del(int id);

        public Task<List<string>> Get(string type, string userid);
    }
    public sealed partial class WhiteListDelInfo
    {
        public string SecretKey { get; set; }
        public int Id { get; set; }
    }
    public sealed partial class WhiteListAddInfo
    {
        public string SecretKey { get; set; }
        public WhiteListInfo Data { get; set; }
    }
    public sealed partial class WhiteListPageRequestInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public string Type { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public string SecretKey { get; set; }
    }
    public sealed partial class WhiteListPageResultInfo
    {
        public int Page { get; set; }
        public int Size { get; set; }
        public int Count { get; set; }
        public List<WhiteListInfo> List { get; set; }
    }
    public sealed partial class WhiteListInfo
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public DateTime AddTime { get; set; } = DateTime.Now;

        public string[] Nodes { get; set; } = [];
    }

    public sealed class WhiteListConfigInfo
    {
        /// <summary>
        /// 加解密密钥
        /// </summary>
#if DEBUG
        public string SecretKey { get; set; } = Helper.GlobalString;
#else
        public string SecretKey { get; set; } = Guid.NewGuid().ToString().ToUpper();
#endif
    }
}
