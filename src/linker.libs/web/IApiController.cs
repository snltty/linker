using linker.libs.websocket;
using System;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

namespace linker.libs.web
{
    /// <summary>
    /// 前段接口
    /// </summary>
    public interface IApiController { }

    /// <summary>
    /// 前段接口response
    /// </summary>
    public sealed class ApiControllerResponseInfo
    {
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// 请求id
        /// </summary>
        public long RequestId { get; set; }
        /// <summary>
        /// 状态码
        /// </summary>
        public ApiControllerResponseCodes Code { get; set; } = ApiControllerResponseCodes.Success;
        /// <summary>
        /// 数据
        /// </summary>
        public object Content { get; set; } = string.Empty;

        [JsonIgnore]
        public WebSocket Connection { get; set; }
    }

    /// <summary>
    /// 前端接口request
    /// </summary>
    public sealed class ApiControllerRequestInfo
    {
        [JsonIgnore]
        public WebSocket Connection { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// 请求id
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// 前端接口执行参数
    /// </summary>
    public sealed class ApiControllerParamsInfo
    {
        public WebSocket Connection { get; set; }
        /// <summary>
        /// 请求id
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public string Content { get; set; } = string.Empty;
        /// <summary>
        /// 状态码
        /// </summary>
        public ApiControllerResponseCodes Code { get; private set; } = ApiControllerResponseCodes.Success;
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// 设置状态码
        /// </summary>
        /// <param name="code"></param>
        /// <param name="errormsg"></param>
        public void SetCode(ApiControllerResponseCodes code, string errormsg = "")
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
            Code = ApiControllerResponseCodes.Error;
            ErrorMessage = msg;
        }
    }
    /// <summary>
    /// 前端接口状态码
    /// </summary>
    public enum ApiControllerResponseCodes : byte
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
    public class ApiControllerAttribute : Attribute
    {
        /// <summary>
        /// 参数类型
        /// </summary>
        public Type Param { get; set; }
        public ApiControllerAttribute(Type param)
        {
            Param = param;
        }
    }
}
