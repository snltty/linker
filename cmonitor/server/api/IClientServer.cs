using cmonitor.server.api.websocket;
using System.Reflection;

namespace cmonitor.server.api
{
    /// <summary>
    /// 前端接口服务
    /// </summary>
    public interface IClientServer
    {
        /// <summary>
        /// websocket
        /// </summary>
        public void Websocket();
        /// <summary>
        /// 加载插件
        /// </summary>
        /// <param name="assemblys"></param>
        public void LoadPlugins(Assembly[] assemblys);
        public void Notify(string path, object content);
        public void Notify(string path,string name, Memory<byte> content);
        public void Notify(string path, object content,WebsocketConnection connection);
    }

}
