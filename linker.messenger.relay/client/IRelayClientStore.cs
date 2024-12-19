using linker.messenger.relay.client.transport;
using System.Security.Cryptography.X509Certificates;

namespace linker.messenger.relay.client
{
    public interface IRelayClientStore
    {
        /// <summary>
        /// 标志，当所有业务使用同一端口时，flag区分，0则不发送
        /// </summary>
        public byte Flag { get; }
        /// <summary>
        /// 加密证书
        /// </summary>
        public X509Certificate2 Certificate { get; }
        /// <summary>
        /// 登录连接
        /// </summary>
        public IConnection SigninConnection { get; }


        /// <summary>
        /// 密钥
        /// </summary>
        public string SecretKey { get;  }
        /// <summary>
        /// 禁用
        /// </summary>
        public bool Disabled { get; }
        /// <summary>
        /// 开启ssl
        /// </summary>
        public bool SSL { get;}
        /// <summary>
        /// 中继类型
        /// </summary>
        public RelayClientType RelayType { get;  }
    }
}
