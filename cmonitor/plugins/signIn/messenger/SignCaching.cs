using cmonitor.db;
using cmonitor.server;
using common.libs;
using common.libs.database;
using LiteDB;
using MemoryPack;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;

namespace cmonitor.plugins.signin.messenger
{
    public sealed class SignCaching
    {
        private readonly DBfactory dBfactory;
        private readonly ILiteCollection<SignCacheInfo> liteCollection;
        public ConcurrentDictionary<string, SignCacheInfo> Clients { get; set; } = new ConcurrentDictionary<string, SignCacheInfo>();

        public SignCaching(DBfactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<SignCacheInfo>("signs");

            foreach (var item in liteCollection.FindAll())
            {
                item.Connected = false;
                Clients.TryAdd(item.MachineName, item);
            }
        }

        public void Sign(IConnection connection, SignInfo signInfo)
        {
            if (Clients.TryRemove(signInfo.MachineName, out SignCacheInfo cache))
            {
                cache.Connection?.Disponse(9);
                liteCollection.Delete(cache.Id);
            }
            connection.Name = signInfo.MachineName;
            SignCacheInfo cache1 = new SignCacheInfo
            {
                Connection = connection,
                MachineName = signInfo.MachineName,
                Version = signInfo.Version,
                Args = signInfo.Args,
                GroupId = signInfo.GroupId,
            };
            Clients.TryAdd(signInfo.MachineName, cache1);
            liteCollection.Insert(cache1);

        }
        public bool Get(string machineName, out SignCacheInfo cache)
        {
            if (machineName == null)
            {
                cache = null;
                return false;
            }
            return Clients.TryGetValue(machineName, out cache);
        }

        public List<SignCacheInfo> Get(string groupId)
        {
            return Clients.Values.Where(c => c.GroupId == groupId).ToList();
        }

        public bool Del(string machineName)
        {
            if (Clients.TryRemove(machineName, out SignCacheInfo cache))
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
        public string MachineName { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
}
