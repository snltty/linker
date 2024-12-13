using linker.config;
using linker.libs;

namespace linker.plugins.access
{
    public sealed class AccessTransfer
    {
        public ClientApiAccess Access => fileConfig.Data.Client.Access;

        public Action OnChanged { get; set; } = () => { };

        private readonly FileConfig fileConfig;
        public AccessTransfer(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;
        }

        /// <summary>
        /// 设置权限
        /// </summary>
        /// <param name="info"></param>
        public void SetAccess(ConfigUpdateAccessInfo info)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access},my access {(ulong)fileConfig.Data.Client.Access}");

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access} success");
            fileConfig.Data.Client.Access = (ClientApiAccess)info.Access;
            fileConfig.Data.Update();
            OnChanged();
        }

        /// <summary>
        /// 合并权限
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        public ClientApiAccess AssignAccess(ClientApiAccess access)
        {
            return fileConfig.Data.Client.Access & access;
        }

        /// <summary>
        /// 是否拥有某项权限
        /// </summary>
        /// <param name="clientManagerAccess"></param>
        /// <returns></returns>
        public bool HasAccess(ClientApiAccess clientManagerAccess)
        {
            return (fileConfig.Data.Client.Access & clientManagerAccess) == clientManagerAccess;
        }
    }
}
