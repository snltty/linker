namespace linker.messenger.api
{
    public interface IAccessStore
    {
        /// <summary>
        /// 权限值
        /// </summary>
        public AccessValue Access { get; }

        /// <summary>
        /// 发生变化
        /// </summary>
        public Action OnChanged { get; set; }

        /// <summary>
        /// 设置权限
        /// </summary>
        /// <param name="info"></param>
        public void SetAccess(AccessUpdateInfo info);
        /// <summary>
        /// 合并权限
        /// </summary>
        /// <param name="access">将access与自身拥有的权限进行合并</param>
        /// <returns></returns>
        public AccessValue AssignAccess(AccessValue access);
        /// <summary>
        /// 是否拥有指定权限
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        public bool HasAccess(AccessValue access);
    }
}
