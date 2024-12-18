using linker.libs;
using System.Collections.Concurrent;
using System.Net;
using linker.messenger;

namespace linker.messenger.signin
{
    public sealed class SignCaching
    {
        private readonly SignInArgsTransfer signInArgsTransfer;
        private readonly ISignInStore signInStore;

        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();

        public SignCaching(ISignInStore signInStore, SignInArgsTransfer signInArgsTransfer)
        {
            this.signInStore = signInStore;
            this.signInArgsTransfer = signInArgsTransfer;
            try
            {
                foreach (var item in signInStore.Find())
                {
                    item.Connected = false;
                    Clients.TryAdd(item.MachineId, item);
                }
            }
            catch (Exception)
            {
            }
            ClearTask();
        }

        public async Task<string> Sign(SignInfo signInfo)
        {
            if (string.IsNullOrWhiteSpace(signInfo.MachineId))
            {
                signInfo.MachineId = signInStore.NewId();
            }

            bool has = Clients.TryGetValue(signInfo.MachineId, out SignCacheInfo cache);
            if (has == false)
            {
                cache = new SignCacheInfo();
            }

            //参数验证失败
            string verifyResult = await signInArgsTransfer.Verify(signInfo, cache);
            if (string.IsNullOrWhiteSpace(verifyResult) == false)
            {
                cache.Connected = false;
                return verifyResult;
            }
            //无限制，则挤压下线
            cache.Connection?.Disponse(9);
            if (has == false)
            {
                cache.Id = signInfo.MachineId;
                cache.MachineId = signInfo.MachineId;
                signInStore.Insert(cache);
                signInStore.Confirm();
                Clients.TryAdd(signInfo.MachineId, cache);
            }

            signInfo.Connection.Id = signInfo.MachineId;
            signInfo.Connection.Name = signInfo.MachineName;
            cache.MachineName = signInfo.MachineName;
            cache.Connection = signInfo.Connection;
            cache.Version = signInfo.Version;
            cache.Args = signInfo.Args;
            cache.GroupId = signInfo.GroupId;
            signInStore.Update(cache);
            signInStore.Confirm();

            return string.Empty;
        }

        public bool TryGet(string machineId, out SignCacheInfo cache)
        {
            if (machineId == null)
            {
                cache = null;
                return false;
            }
            return Clients.TryGetValue(machineId, out cache);
        }

        public List<SignCacheInfo> Get()
        {
            return Clients.Values.ToList();
        }
        public List<SignCacheInfo> Get(string groupId)
        {
            return Clients.Values.Where(c => c.GroupId == groupId).ToList();
        }

        public bool GetOnline(string machineId)
        {
            return Clients.TryGetValue(machineId, out SignCacheInfo cache) && cache.Connected;
        }
        public void GetOnline(out int all, out int online)
        {
            all = Clients.Count;
            online = Clients.Values.Count(c => c.Connected);
        }

        public bool TryRemove(string machineId, out SignCacheInfo cache)
        {
            if (Clients.TryRemove(machineId, out cache))
            {
                signInStore.Delete(cache.Id);
                signInStore.Confirm();
            }
            return true;
        }

        public string NewId()
        {
            return signInStore.NewId();
        }

        private void ClearTask()
        {
            TimerHelper.SetInterval(() =>
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"start cleaning up clients that have exceeded the 7-day timeout period");
                }

                try
                {
                    DateTime now = DateTime.Now;

                    var groups = Clients.Values.GroupBy(c => c.GroupId)
                     .Where(group => group.All(info => info.Connected == false && (now - info.LastSignIn).TotalDays > 7))
                     .Select(group => group.Key).ToList();

                    if (groups.Count > 0)
                    {
                        var items = Clients.Values.Where(c => groups.Contains(c.GroupId)).ToList();

                        foreach (var item in items)
                        {
                            Clients.TryRemove(item.MachineId, out _);
                            signInStore.Delete(item.Id);
                        }
                        signInStore.Confirm();
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Debug($"cleaning up clients error {ex}");
                }

                return true;
            }, 5 * 60 * 1000);
        }
    }

    public sealed class SignCacheInfo
    {
        public string Id { get; set; }

        public string MachineId { get; set; }
        public string MachineName { get; set; }
        public string Version { get; set; } = "v1.0.0";
        public string GroupId { get; set; } = Helper.GlobalString;
        public DateTime LastSignIn { get; set; } = DateTime.Now;
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

        private IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
        public IPEndPoint IP
        {
            get
            {
                if (Connection != null)
                {
                    ip = Connection.Address;
                }
                return ip;
            }
            set
            {
                ip = value;
            }
        }

        private bool connected = false;
        public bool Connected
        {
            get
            {
                if (Connection != null)
                {
                    connected = Connection.Connected == true;
                }
                return connected;
            }
            set
            {
                connected = value;
            }
        }

        public IConnection Connection { get; set; }

        public uint Order { get; set; } = int.MaxValue;
    }

    public sealed class SignInfo
    {
        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

        public IConnection Connection { get; set; }
    }
}
