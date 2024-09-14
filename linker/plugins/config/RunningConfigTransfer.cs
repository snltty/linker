using linker.libs;
using linker.plugins.client;
using linker.plugins.config.messenger;
using linker.plugins.messenger;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        /// <summary>
        /// 同步配置的版本记录
        /// </summary>
        public Dictionary<string, ulong> Versions { get; set; } = new Dictionary<string, ulong>();
        public Dictionary<string, bool> DisableSyncs { get; set; } = new Dictionary<string, bool>();
    }

    [MemoryPackable]
    public sealed partial class ConfigVersionInfo
    {
        /// <summary>
        /// 配置key
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 配置版本
        /// </summary>
        public ulong Version { get; set; }
        /// <summary>
        /// 配置数据
        /// </summary>
        public Memory<byte> Data { get; set; }
    }

    /// <summary>
    /// 配置同步
    /// </summary>
    public sealed class RunningConfigTransfer
    {
        private ConcurrentDictionary<string, Action<Memory<byte>>> setters = new ConcurrentDictionary<string, Action<Memory<byte>>>();
        private ConcurrentDictionary<string, Func<Memory<byte>>> getters = new ConcurrentDictionary<string, Func<Memory<byte>>>();

        private readonly RunningConfig runningConfig;
        private readonly MessengerSender sender;
        private readonly ClientSignInState clientSignInState;
        public RunningConfigTransfer(RunningConfig runningConfig, MessengerSender sender, ClientSignInState clientSignInState)
        {
            this.runningConfig = runningConfig;
            this.sender = sender;
            this.clientSignInState = clientSignInState;
        }
        /// <summary>
        /// 设置配置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        public void Setter(string key, Action<Memory<byte>> callback)
        {
            setters.TryAdd(key, callback);
        }
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="callback"></param>
        public void Getter(string key, Func<Memory<byte>> callback)
        {
            getters.TryAdd(key, callback);
        }

        /// <summary>
        /// 输入配置
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Memory<byte> InputConfig(ConfigVersionInfo info)
        {
            if (GetDisableSync(info.Key))
            {
                return Helper.EmptyArray;
            }
            ulong version = GetVersion(info.Key);

            if (setters.TryGetValue(info.Key, out Action<Memory<byte>> setter) && info.Version > version)
            {
                try
                {
                    setter(info.Data);
                    UpdateVersion(info.Key, info.Version);
                }
                catch (Exception)
                {
                }
            }
            else if (getters.TryGetValue(info.Key, out Func<Memory<byte>> getter) && version > info.Version)
            {
                return MemoryPackSerializer.Serialize(new ConfigVersionInfo
                {
                    Data = getter(),
                    Key = info.Key,
                    Version = version
                });
            }

            return Helper.EmptyArray;
        }


        private object syncLockObj = new();
        /// <summary>
        /// 同步配置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public void Sync(string key, Memory<byte> data)
        {
            if (GetDisableSync(key))
            {
                return;
            }

            ulong version = GetVersion(key);
            sender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)(ConfigMessengerIds.UpdateForward),
                Payload = MemoryPackSerializer.Serialize(new ConfigVersionInfo
                {
                    Key = key,
                    Data = data,
                    Version = version
                })
            }).ContinueWith((result) =>
            {
                lock (syncLockObj)
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        ConfigVersionInfo info = MemoryPackSerializer.Deserialize<ConfigVersionInfo>(result.Result.Data.Span);
                        if (setters.TryGetValue(info.Key, out Action<Memory<byte>> setter) && info.Version > GetVersion(info.Key))
                        {
                           
                            try
                            {
                                setter(info.Data);
                                UpdateVersion(info.Key, info.Version);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            });
        }

        private ulong GetVersion(string key)
        {
            if (runningConfig.Data.Versions.TryGetValue(key, out ulong version) == false)
            {
                version = 1;
                runningConfig.Data.Versions[key] = version;
                runningConfig.Data.Update();
            }
            return version;
        }
        /// <summary>
        /// 更新版本
        /// </summary>
        /// <param name="key"></param>
        /// <param name="version"></param>
        public void UpdateVersion(string key, ulong version)
        {
            runningConfig.Data.Versions[key] = version;
            runningConfig.Data.Update();
        }
        /// <summary>
        /// 版本+1
        /// </summary>
        /// <param name="key"></param>
        public void IncrementVersion(string key)
        {
            ulong version = GetVersion(key);
            UpdateVersion(key, version + 1);
        }

        private bool GetDisableSync(string key)
        {
            return runningConfig.Data.DisableSyncs.TryGetValue(key, out bool sync) && sync;
        }
        public void UpdateDisableSync(string key, bool sync)
        {
            runningConfig.Data.DisableSyncs[key] = sync;
            runningConfig.Data.Update();
        }
    }
}
