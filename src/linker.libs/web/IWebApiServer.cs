using System;
using System.Collections.Generic;

namespace linker.libs.web
{
    /// <summary>
    /// web服务
    /// </summary>
    public interface IWebApiServer
    {
        /// <summary>
        /// 开始
        /// </summary>
        public void Start(int port);

        public void AddController(IWebApiController controller);
        public void AddControllers(List<IWebApiController> controllers);
    }

    public interface IWebApiController
    {
        public string Path { get; }
        public Memory<byte> Handle(string query);
        public void Free();
    }

}
