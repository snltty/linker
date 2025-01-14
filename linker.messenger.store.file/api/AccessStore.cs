using linker.libs;
using linker.messenger.api;

namespace linker.messenger.store.file.api
{
    public sealed class AccessStore : IAccessStore
    {
        public AccessValue Access => fileConfig.Data.Client.Access;
        public Action OnChanged { get; set; } = () => { };

        private readonly FileConfig fileConfig;
        public AccessStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        /// <summary>
        /// 设置权限
        /// </summary>
        /// <param name="info"></param>
        public void SetAccess(AccessUpdateInfo info)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access},my access {(ulong)fileConfig.Data.Client.Access}");

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access} success");
            fileConfig.Data.Client.Access = (AccessValue)info.Access;
            fileConfig.Data.Update();

            OnChanged();
        }

        /// <summary>
        /// 合并权限
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        public AccessValue AssignAccess(AccessValue access)
        {
            return fileConfig.Data.Client.Access & access;
        }

        /// <summary>
        /// 是否拥有某项权限
        /// </summary>
        /// <param name="clientManagerAccess"></param>
        /// <returns></returns>
        public bool HasAccess(AccessValue clientManagerAccess)
        {
            return (fileConfig.Data.Client.Access & clientManagerAccess) == clientManagerAccess;
        }
    }
}
