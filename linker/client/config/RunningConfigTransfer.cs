using linker.client.config.messenger;
using linker.libs;
using linker.server;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.client.config
{
    public sealed partial class RunningConfigInfo
    {
        public Dictionary<string, ulong> Versions { get; set; } = new Dictionary<string, ulong>();
    }

    [MemoryPackable]
    public sealed partial class ConfigVersionInfo
    {
        public string Key { get; set; }
        public ulong Version { get; set; }
        public Memory<byte> Data { get; set; }
    }

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
        public void Setter(string key, Action<Memory<byte>> callback)
        {
            setters.TryAdd(key, callback);
        }
        public void Getter(string key, Func<Memory<byte>> callback)
        {
            getters.TryAdd(key, callback);
        }

        public Memory<byte> InputConfig(ConfigVersionInfo info)
        {
            ulong version = GetVersion(info.Key);

            if (setters.TryGetValue(info.Key, out Action<Memory<byte>> setter) && info.Version > version)
            {
                UpdateVersion(info.Key, info.Version);
                setter(info.Data);
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
        public void Sync(string key, Memory<byte> data)
        {
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
                            UpdateVersion(info.Key, info.Version);
                            setter(info.Data);
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
        public void UpdateVersion(string key, ulong version)
        {
            runningConfig.Data.Versions[key] = version;
            runningConfig.Data.Update();
        }
        public void IncrementVersion(string key)
        {
            ulong version = GetVersion(key);
            UpdateVersion(key, version + 1);
        }
    }
}
