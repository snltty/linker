using link.database;
using link.server;
using link.libs;
using LiteDB;
using MemoryPack;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Serialization;

namespace link.plugins.signin.messenger
{
    public sealed class SignCaching
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<SignCacheInfo> liteCollection;
        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();

        public SignCaching(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<SignCacheInfo>("signs");

            foreach (var item in liteCollection.FindAll())
            {
                item.Connected = false;
                Clients.TryAdd(item.MachineId, item);
            }
        }

        public void Sign(IConnection connection, SignInfo signInfo)
        {
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
            cache.Connection?.Disponse(9);

            connection.Id = signInfo.MachineId;
            connection.Name = signInfo.MachineName;
            cache.MachineName = signInfo.MachineName;
            cache.Connection = connection;
            cache.Version = signInfo.Version;
            cache.Args = signInfo.Args;
            cache.GroupId = signInfo.GroupId;
            liteCollection.Update(cache);
            dBfactory.Confirm();
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
    }


    [MemoryPackable]
    public sealed partial class SignInfo
    {

        public string MachineId { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
}
