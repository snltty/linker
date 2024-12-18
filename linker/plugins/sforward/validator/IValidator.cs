using linker.messenger.signin;
using linker.plugins.sforward.config;

namespace linker.plugins.sforward.validator
{
    public interface ISForwardValidator
    {
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="signCacheInfo">来源客户端</param>
        /// <param name="sForwardAddInfo">穿透信息</param>
        /// <returns></returns>
        public Task<string> Validate(SignCacheInfo signCacheInfo, SForwardAddInfo sForwardAddInfo);
    }
}
