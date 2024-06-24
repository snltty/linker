namespace Linker.Libs.Web
{
    /// <summary>
    /// web服务
    /// </summary>
    public interface IWebServer
    {
        /// <summary>
        /// 开始
        /// </summary>
        public void Start(int port, string root);
    }

}
