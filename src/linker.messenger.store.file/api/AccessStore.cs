using linker.libs;
using linker.messenger.api;
using System.Collections;

namespace linker.messenger.store.file.api
{
    public sealed class AccessStore : IAccessStore
    {
        public BitArray AccessBits => fileConfig.Data.Client.AccessBits;
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
        public void SetAccess(AccessBitsUpdateInfo info)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access},my access {AccessBits}");

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"from {info.FromMachineId} set access to {info.Access} success");
            fileConfig.Data.Client.AccessBits = info.Access;
            fileConfig.Data.Update();

            OnChanged();
        }

        /// <summary>
        /// 合并权限
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        public BitArray AssignAccess(BitArray access)
        {
            if (AccessBits.Length != access.Length)
            {
                int maxLength = Math.Max(AccessBits.Length, access.Length);
                int[] newArray = new int[maxLength];

                if (AccessBits.Length != maxLength)
                {
                    AccessBits.CopyTo(newArray, 0);
                    fileConfig.Data.Client.AccessBits = new BitArray(newArray);
                    fileConfig.Data.Update();
                }
                else
                {
                    access.CopyTo(newArray, 0);
                    access = new BitArray(newArray);
                }
            }

            return access.And(AccessBits);
        }

        /// <summary>
        /// 是否拥有某项权限
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        public bool HasAccess(AccessValue access)
        {
            int index = (int)access;
            return AccessBits.HasAllSet() || (AccessBits.Length > index && AccessBits.Get(index));
        }
    }
}
