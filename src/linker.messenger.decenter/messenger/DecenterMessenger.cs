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
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, DecenterCacheInfo>> decenters = new ConcurrentDictionary<string, ConcurrentDictionary<int, DecenterCacheInfo>>();

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
                if (decenters.TryGetValue(info.Name, out ConcurrentDictionary<int, DecenterCacheInfo> dic) == false)
                {
                    dic = new ConcurrentDictionary<int, DecenterCacheInfo>();
                    decenters.TryAdd(info.Name, dic);
                }
                if (dic.TryGetValue(connection.GetHashCode(), out DecenterCacheInfo cache) == false)
                {
                    cache = new DecenterCacheInfo();
                    dic.TryAdd(connection.GetHashCode(), cache);
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

            KeyValuePair<string, bool> info;
            try
            {
                info = serializer.Deserialize<KeyValuePair<string, bool>>(connection.ReceiveRequestWrap.Payload.Span);
            }
            catch
            {
                info = new KeyValuePair<string, bool>(serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span), true);
            }

            if (decenters.TryGetValue(info.Key, out ConcurrentDictionary<int, DecenterCacheInfo> dic) == false)
            {
                connection.Write(Helper.FalseArray);
                return;
            }

            IEnumerable<Memory<byte>> data = dic.Where(c =>
            {
                c.Value.Versions.TryGetValue(connection.GetHashCode(), out DecenterCacheVersionInfo version);
                bool result = c.Key != connection.GetHashCode()
                && c.Value.SignIn.SameGroup(signin)
                && (info.Value || version == null || c.Value.Version.Eq(version.Value, out ulong newVersion) == false);

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
            if (decenters.TryGetValue(name, out ConcurrentDictionary<int, DecenterCacheInfo> dic) && dic.ContainsKey(connection.GetHashCode()))
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
                foreach (ConcurrentDictionary<int, DecenterCacheInfo> dic in decenters.Values)
                {
                    foreach (int id in dic.Where(c => c.Value.SignIn.Connection.Connected == false).Select(c => c.Key).ToList())
                    {
                        dic.TryRemove(id, out _);
                    }
                }
                List<DecenterCacheInfo> versions = decenters.Values.SelectMany(c => c.Values).Where(c => c.Versions.Values.Any(c => c.Connection.Connected == false)).ToList();
                foreach (DecenterCacheInfo cache in versions)
                {
                    foreach (int hashcode in cache.Versions.Where(c => c.Value.Connection.Connected == false).Select(c => c.Key).ToList())
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
