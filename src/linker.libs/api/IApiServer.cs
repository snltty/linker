namespace linker.libs.api
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
        public void SetPassword(string password);
    }

}
