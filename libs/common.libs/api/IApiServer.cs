using common.libs.websocket;
using System;

namespace common.libs.api
{
    /// <summary>
    /// 前端接口服务
    /// </summary>
    public interface IApiServer
    {
        /// <summary>
        /// websocket
        /// </summary>
        public void Websocket(int port,string password);
        public void Notify(string path, object content);
        public void Notify(string path,string name, ReadOnlyMemory<byte> content);
        public void Notify(string path, object content,WebsocketConnection connection);
    }

}
