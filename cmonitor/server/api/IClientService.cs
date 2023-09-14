using cmonitor.server.api.websocket;
using System.Text.Json.Serialization;

namespace cmonitor.server.api
{
    /// <summary>
    /// 前段接口
    /// </summary>
    public interface IClientService { }

    /// <summary>
    /// 前段接口response
    /// </summary>
    public sealed class ClientServiceResponseInfo
    {
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// 请求id
        /// </summary>
        public long RequestId { get; set; } = 0;
        /// <summary>
        /// 状态码
        /// </summary>
        public ClientServiceResponseCodes Code { get; set; } = ClientServiceResponseCodes.Success;
        /// <summary>
        /// 数据
        /// </summary>
        public object Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// 前端接口request
    /// </summary>
    public sealed class ClientServiceRequestInfo
    {
        [JsonIgnore]
        public WebsocketConnection Connection { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// 请求id
        /// </summary>
        public uint RequestId { get; set; } = 0;
        /// <summary>
        /// 数据
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// 前端接口执行参数
    /// </summary>
    public sealed class ClientServiceParamsInfo
    {
        public WebsocketConnection Connection { get; set; }
        /// <summary>
        /// 请求id
        /// </summary>
        public uint RequestId { get; set; } = 0;
        /// <summary>
        /// 数据
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// 状态码
        /// </summary>
        public ClientServiceResponseCodes Code { get; private set; } = ClientServiceResponseCodes.Success;
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// 设置状态码
        /// </summary>
        /// <param name="code"></param>
        /// <param name="errormsg"></param>
        public void SetCode(ClientServiceResponseCodes code, string errormsg = "")
        {
            Code = code;
            ErrorMessage = errormsg;
        }
        /// <summary>
        /// 设置错误信息
        /// </summary>
        /// <param name="msg"></param>
        public void SetErrorMessage(string msg)
        {
            Code = ClientServiceResponseCodes.Error;
            ErrorMessage = msg;
        }
    }
    /// <summary>
    /// 前端接口状态码
    /// </summary>
    public enum ClientServiceResponseCodes : byte
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,
        /// <summary>
        /// 没找到
        /// </summary>
        NotFound = 1,
        /// <summary>
        /// 失败
        /// </summary>
        Error = 0xff,

    }

    /// <summary>
    /// 前端接口标识特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientServiceAttribute : Attribute
    {
        /// <summary>
        /// 参数类型
        /// </summary>
        public Type Param { get; set; }
        public ClientServiceAttribute(Type param)
        {
            Param = param;
        }
    }
}
