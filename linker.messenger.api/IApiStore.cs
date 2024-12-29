using linker.libs;
namespace linker.messenger.api
{
    public sealed class ApiClientInfo
    {
        /// <summary>
        /// 管理接口端口
        /// </summary>
        public int ApiPort { get; set; } = 1803;
        /// <summary>
        /// 管理接口密码
        /// </summary>
        public string ApiPassword { get; set; } = Helper.GlobalString;

        /// <summary>
        /// 网站端口
        /// </summary>
        public int WebPort { get; set; } = 1804;
        /// <summary>
        /// 网站根目录
        /// </summary>
        public string WebRoot { get; set; } = "./web/";
    }

    public interface IApiStore
    {
        public ApiClientInfo Info { get; }
    }
}
