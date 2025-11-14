using linker.libs;
using linker.libs.timer;
using linker.messenger.signin.args;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;

namespace linker.messenger.signin
{
    /// <summary>
    /// 登录缓存
    /// </summary>
    public sealed class SignInServerCaching
    {
        private readonly SignInArgsTransfer signInArgsTransfer;
        private readonly ISignInServerStore signInStore;

        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();

        public SignInServerCaching(ISignInServerStore signInStore, SignInArgsTransfer signInArgsTransfer)
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
            string verifyResult = await signInArgsTransfer.Validate(signInfo, cache).ConfigureAwait(false);
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
            cache.Super = signInfo.Super;
            cache.UserId = signInfo.UserId;

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
        public bool TryGet(string from, string to, out SignCacheInfo fromValue, out SignCacheInfo toValue)
        {
            fromValue = null;
            toValue = null;
            if (from == null || to == null)
            {
                return false;
            }
            return Clients.TryGetValue(from, out fromValue) && Clients.TryGetValue(to, out toValue) && fromValue.SameGroup(toValue);
        }

        public List<SignCacheInfo> Get()
        {
            return Clients.Values.ToList();
        }
        public List<SignCacheInfo> Get(string name, int count)
        {
            return Clients.Values.Where(c => c.MachineName.Contains(name) || (c.MachineId != null && c.MachineId.Contains(name))).Take(count).ToList();
        }
        public List<SignCacheInfo> Get(SignCacheInfo other)
        {
            return Clients.Values.Where(c => c.GroupId == other.GroupId).ToList();
        }
        public List<string> GetUserIds(List<string> ids)
        {
            return Clients.Values.Where(c => ids.Contains(c.MachineId)).Select(c => c.UserId).Distinct().ToList();
        }

        public IEnumerable<string> GetOnline()
        {
            return Clients.Values.Where(c => c.Connected).Select(c => c.MachineId);
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

        public string[] Exp(string machineId)
        {
            if (TryGet(machineId, out SignCacheInfo cache))
            {
                cache.LastSignIn = DateTime.Now;
            }
            return signInStore.Exp(machineId);
        }

        private void ClearTask()
        {
            TimerHelper.SetIntervalLong(() =>
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Debug($"start cleaning up clients that have exceeded the {signInStore.CleanDays}-day timeout period");
                }

                try
                {
                    DateTime now = DateTime.Now;

                    var groups = Clients.Values.GroupBy(c => c.GroupId)
                     .Where(group => group.All(info => info.Connected == false && (now - info.LastSignIn).TotalDays > signInStore.CleanDays))
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

            }, 5 * 60 * 1000);
        }
    }

    /// <summary>
    /// 登录缓存对象
    /// </summary>
    public partial class SignCacheInfo
    {
        public string Id { get; set; }
        /// <summary>
        /// 客户端id
        /// </summary>
        public string MachineId { get; set; }
        /// <summary>
        /// 客户端名
        /// </summary>
        public string MachineName { get; set; }
        /// <summary>
        /// 客户端版本
        /// </summary>
        public string Version { get; set; } = "v1.0.0";
        /// <summary>
        /// 分组编号
        /// </summary>
        public string GroupId { get; set; } = Helper.GlobalString;
        /// <summary>
        /// 最后登录
        /// </summary>
        public DateTime LastSignIn { get; set; } = DateTime.Now;
        /// <summary>
        /// 额外参数，就是ISignInArgs
        /// </summary>
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

        private bool connected;
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

        [JsonIgnore]
        public bool Super { get; set; }
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;


        /// <summary>
        /// 连接对象
        /// </summary>
        [JsonIgnore]
        public IConnection Connection { get; set; }
        [JsonIgnore]
        public uint Order { get; set; } = int.MaxValue;

        public bool SameGroup(SignCacheInfo other)
        {
            return this.GroupId == other.GroupId;
        }
    }

    /// <summary>
    /// 登录参数
    /// </summary>
    public partial class SignInfo
    {
        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

        public IConnection Connection { get; set; }
    }
}
