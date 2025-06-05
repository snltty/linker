using linker.libs;
namespace linker.messenger.api
{
    public sealed class ApiClientInfo
    {
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

        /// <summary>
        /// 设置
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool Set(ApiClientInfo info);
        /// <summary>
        /// 设置接口密码
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool SetApiPassword(string password);
        /// <summary>
        /// 设置网页端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool SetWebPort(int port);
        /// <summary>
        /// 设置网页根目录
        /// </summary>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public bool SetWebRoot(string rootPath);
        /// <summary>
        /// 提交保存
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
