using System.Collections;

namespace linker.messenger.api
{
    public interface IAccessStore
    {
        public BitArray AccessBits { get; }

        /// <summary>
        /// 发生变化
        /// </summary>
        public Action OnChanged { get; set; }

        /// <summary>
        /// 设置权限
        /// </summary>
        /// <param name="info"></param>
        public void SetAccess(AccessBitsUpdateInfo info);
        /// <summary>
        /// 合并权限
        /// </summary>
        /// <param name="access">将access与自身拥有的权限进行合并</param>
        /// <returns></returns>
        public BitArray AssignAccess(BitArray access);
        /// <summary>
        /// 是否拥有指定权限
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        public bool HasAccess(AccessValue access);
    }
}
