using linker.libs.extends;
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
            fileConfig.Data.Client.FullAccess = info.FullAccess;
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
            if (AccessBits.Count != access.Length)
            {
                int maxLength = Math.Max(AccessBits.Count, access.Length);

                fileConfig.Data.Client.AccessBits = AccessBits.PadRight(maxLength, fileConfig.Data.Client.FullAccess);
                fileConfig.Data.Update();

                access = access.PadRight(maxLength, false);
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
            return fileConfig.Data.Client.FullAccess || (AccessBits.Count > index && AccessBits.Get(index));
        }
    }
}
