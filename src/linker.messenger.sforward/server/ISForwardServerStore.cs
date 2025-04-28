namespace linker.messenger.sforward.server
{
    public interface ISForwardServerStore
    {
        /// <summary>
        /// 缓冲区大小
        /// </summary>
        public byte BufferSize { get; }
        /// <summary>
        /// web端口
        /// </summary>
        public int WebPort { get; }
        /// <summary>
        /// 端口隧道范围
        /// </summary>
        public int[] TunnelPortRange { get; }

        public bool ValidateSecretKey(string key);
        /// <summary>
        /// 穿透密钥
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool SetSecretKey(string key);
        /// <summary>
        /// 缓冲区大小
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool SetBufferSize(byte size);
        /// <summary>
        /// web端口
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool SetWebPort(int port);
        /// <summary>
        /// 端口隧道范围
        /// </summary>
        /// <param name="ports"></param>
        /// <returns></returns>
        public bool SetTunnelPortRange(int[] ports);

        /// <summary>
        /// 提交保存
        /// </summary>
        /// <returns></returns>
        public bool Confirm();
    }
}
