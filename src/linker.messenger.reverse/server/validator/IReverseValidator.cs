using linker.messenger.signin;

namespace linker.messenger.reverse.server.validator
{
    public interface IReverseValidator
    {
        public string Name { get; }
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="signCacheInfo">来源客户端</param>
        /// <param name="ReverseAddInfo">穿透信息</param>
        /// <returns></returns>
        public Task<string> Validate(SignCacheInfo signCacheInfo, ReverseAddInfo ReverseAddInfo);
    }
}
