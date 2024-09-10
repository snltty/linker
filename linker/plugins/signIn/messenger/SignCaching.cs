using linker.store;
using linker.libs;
using LiteDB;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;
using linker.plugins.messenger;
using linker.plugins.signIn.args;

namespace linker.plugins.signin.messenger
{
    public sealed class SignCaching
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<SignCacheInfo> liteCollection;
        private readonly SignInArgsTransfer signInArgsTransfer;

        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();

        public SignCaching(Storefactory dBfactory, SignInArgsTransfer signInArgsTransfer)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<SignCacheInfo>("signs");

            this.signInArgsTransfer = signInArgsTransfer;

            foreach (var item in liteCollection.FindAll())
            {
                item.Connected = false;
                Clients.TryAdd(item.MachineId, item);
            }
        }

        public bool Sign(SignInfo signInfo, out string msg)
        {
            msg = string.Empty;
            if (string.IsNullOrWhiteSpace(signInfo.MachineId))
            {
                signInfo.MachineId = ObjectId.NewObjectId().ToString();
            }

            if (Clients.TryGetValue(signInfo.MachineId, out SignCacheInfo cache) == false)
            {
                cache = new SignCacheInfo();
                cache.Id = new ObjectId(signInfo.MachineId);
                cache.MachineId = signInfo.MachineId;
                liteCollection.Insert(cache);
                Clients.TryAdd(signInfo.MachineId, cache);
            }

            //参数验证失败
            if (signInArgsTransfer.Verify(signInfo, cache, out msg) == false)
            {
                signInfo.Connection.Disponse();
                return false;
            }
            //无限制，则挤压下线
            cache.Connection?.Disponse(9);

            signInfo.Connection.Id = signInfo.MachineId;
            signInfo.Connection.Name = signInfo.MachineName;
            cache.MachineName = signInfo.MachineName;
            cache.Connection = signInfo.Connection;
            cache.Version = signInfo.Version;
            cache.Args = signInfo.Args;
            cache.GroupId = signInfo.GroupId;
            liteCollection.Update(cache);
            dBfactory.Confirm();

            return true;
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

        public List<SignCacheInfo> Get(string groupId)
        {
            return Clients.Values.Where(c => c.GroupId == groupId).ToList();
        }

        public bool TryRemove(string machineId, out SignCacheInfo cache)
        {
            if (Clients.TryRemove(machineId, out cache))
            {
                liteCollection.Delete(cache.Id);
                dBfactory.Confirm();
            }
            return true;
        }
    }

    [MemoryPackable]
    public sealed partial class SignCacheInfo
    {
        [MemoryPackIgnore]
        public ObjectId Id { get; set; }

        public string MachineId { get; set; }
        public string MachineName { get; set; }
        public string Version { get; set; } = "1.0.0.0";
        public string GroupId { get; set; } = Helper.GlobalString;
        public DateTime LastSignIn { get; set; } = DateTime.Now;
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

        private IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
        [MemoryPackAllowSerialize]
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

        [MemoryPackIgnore, JsonIgnore, BsonIgnore]
        public IConnection Connection { get; set; }

        [MemoryPackIgnore, JsonIgnore, BsonIgnore]
        public uint Order { get; set; } = int.MaxValue;
    }


    [MemoryPackable]
    public sealed partial class SignInfo
    {

        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

        [MemoryPackIgnore]
        [JsonIgnore]
        public IConnection Connection { get; set; }
    }
}
