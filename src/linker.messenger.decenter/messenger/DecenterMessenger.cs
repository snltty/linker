using linker.libs;
using linker.libs.timer;
using linker.messenger.signin;
using System;
using System.Collections.Concurrent;

namespace linker.messenger.decenter
{
    public sealed class DecenterServerMessenger : IMessenger
    {
        private readonly IMessengerSender sender;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;

        /// <summary>
        /// name machineid data
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DecenterCacheInfo>> decenters = new ConcurrentDictionary<string, ConcurrentDictionary<string, DecenterCacheInfo>>();

        public DecenterServerMessenger(IMessengerSender sender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.sender = sender;
            this.signCaching = signCaching;
            this.serializer = serializer;
            ClearTask();
        }

        [MessengerId((ushort)DecenterMessengerIds.Push)]
        public void Push(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo signin) == false) return;
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);

            bool changed = false;
            lock (decenters)
            {
                if (decenters.TryGetValue(info.Name, out ConcurrentDictionary<string, DecenterCacheInfo> dic) == false)
                {
                    dic = new ConcurrentDictionary<string, DecenterCacheInfo>();
                    decenters.TryAdd(info.Name, dic);
                }
                if (dic.TryGetValue(connection.Id, out DecenterCacheInfo cache) == false)
                {
                    cache = new DecenterCacheInfo();
                    dic.TryAdd(connection.Id, cache);
                }

                changed = cache.Data.Length != info.Data.Length || info.Data.Span.SequenceEqual(cache.Data.Span) == false;

                cache.Data = info.Data;
                cache.SignIn = signin;
                if (changed)
                {
                    cache.Version.Increment();
                }
            }
            if (changed)
            {
                Memory<byte> memory = serializer.Serialize(info);
                List<SignCacheInfo> caches = signCaching.Get(signin).Where(c => c.MachineId != connection.Id && c.Connected).ToList();
                List<Task<bool>> tasks = caches.Select(c => sender.SendOnly(new MessageRequestWrap
                {
                    Connection = c.Connection,
                    MessengerId = (ushort)DecenterMessengerIds.Notify,
                    Payload = memory
                })).ToList();
            }
        }

        [MessengerId((ushort)DecenterMessengerIds.Pull)]
        public void Pull(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo signin) == false) return;

            string name = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (decenters.TryGetValue(name, out ConcurrentDictionary<string, DecenterCacheInfo> dic) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            IEnumerable<Memory<byte>> data = dic.Where(c =>
            {
                c.Value.Versions.TryGetValue(connection.GetHashCode(), out DecenterCacheVersionInfo version);
                bool result = c.Key != connection.Id
                && c.Value.SignIn.SameGroup(signin)
                && (version == null || c.Value.Version.Eq(version.Value, out ulong newVersion) == false);

                if (version == null)
                {
                    version = new DecenterCacheVersionInfo { Connection = connection, Value = c.Value.Version.Value };
                    c.Value.Versions.TryAdd(connection.GetHashCode(), version);
                }
                version.Value = c.Value.Version.Value;

                return result;
            }).Select(c => c.Value.Data);
            connection.Write(serializer.Serialize(data));
        }

        [MessengerId((ushort)DecenterMessengerIds.Check)]
        public void Check(IConnection connection)
        {
            string name = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (decenters.TryGetValue(name, out ConcurrentDictionary<string, DecenterCacheInfo> dic) && dic.ContainsKey(connection.Id))
            {
                connection.Write(Helper.TrueArray);
                return;
            }
            connection.Write(Helper.FalseArray);
        }

        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                List<string> removes = decenters.Values.SelectMany(c => c.Values).Where(c => c.SignIn.Connected == false).Select(c => c.SignIn.Id).ToList();
                foreach (ConcurrentDictionary<string, DecenterCacheInfo> dic in decenters.Values)
                {
                    foreach (string id in removes)
                    {
                        dic.TryRemove(id, out _);
                    }
                }
                List<DecenterCacheInfo> versions = decenters.Values.SelectMany(c => c.Values).Where(c=>c.Versions.Values.Any(c=>c.Connection.Connected == false)).ToList();
                foreach (DecenterCacheInfo cache in versions)
                {
                    foreach (int hashcode in cache.Versions.Where(c => c.Value.Connection.Connected == false).Select(c=>c.Key).ToList())
                    {
                        cache.Versions.TryRemove(hashcode, out _);
                    }
                }

            }, 30000);
        }
    }

    public sealed class DecenterClientMessenger : IMessenger
    {
        private readonly DecenterClientTransfer syncTreansfer;
        private readonly ISerializer serializer;

        public DecenterClientMessenger(DecenterClientTransfer syncTreansfer, ISerializer serializer)
        {
            this.syncTreansfer = syncTreansfer;
            this.serializer = serializer;
        }

        [MessengerId((ushort)DecenterMessengerIds.Notify)]
        public void Notify(IConnection connection)
        {
            DecenterSyncInfo info = serializer.Deserialize<DecenterSyncInfo>(connection.ReceiveRequestWrap.Payload.Span);
            syncTreansfer.Notify(info);
        }
    }

    public sealed class DecenterCacheInfo
    {
        public SignCacheInfo SignIn { get; set; }
        public VersionManager Version { get; set; } = new VersionManager();
        public Memory<byte> Data { get; set; }

        public ConcurrentDictionary<int, DecenterCacheVersionInfo> Versions { get; set; } = new ConcurrentDictionary<int, DecenterCacheVersionInfo>();
    }

    public sealed class DecenterCacheVersionInfo
    {
        public ulong Value { get; set; }
        public IConnection Connection { get; set; }
    }
}
