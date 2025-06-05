using System.Collections.Generic;

namespace linker.libs.web
{
    /// <summary>
    /// web服务
    /// </summary>
    public interface IWebServer
    {
        /// <summary>
        /// 开始
        /// </summary>
        public void Start(int port, string root,string password);
        public void SetPassword(string password);
    }

}
