using linker.messenger.signin;

namespace linker.messenger.sforward.server.validator
{
    public interface ISForwardValidator
    {
        public string Name { get; }
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="signCacheInfo">来源客户端</param>
        /// <param name="sForwardAddInfo">穿透信息</param>
        /// <returns></returns>
        public Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo);
    }
}
