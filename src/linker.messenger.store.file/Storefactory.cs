using linker.libs;
using linker.libs.extends;
using linker.tunnel.connection;
using LiteDB;
using System.Net;

namespace linker.messenger.store.file
{

    /// <summary>
    /// 持久化
    /// </summary>
    public sealed class Storefactory
    {
        LiteDatabase database;
        public Storefactory()
        {
            BsonMapper bsonMapper = new BsonMapper();
            bsonMapper.RegisterType<IPEndPoint>(serialize: (a) => a.ToString(), deserialize: (a) => IPEndPoint.Parse(a.AsString));
            bsonMapper.RegisterType<IPAddress>(serialize: (a) => a.ToString(), deserialize: (a) => IPAddress.Parse(a.AsString));
            bsonMapper.RegisterType<IPAddress[]>(serialize: (a) => a.ToJson(), deserialize: (a) => a.AsString.DeJson<IPAddress[]>());
            bsonMapper.RegisterType<ITunnelConnection>(serialize: (a) => string.Empty, deserialize: (a) => null);
            bsonMapper.RegisterType<IConnection>(serialize: (a) => string.Empty, deserialize: (a) => null);

            database = new LiteDatabase(new ConnectionString($"Filename=./configs/db.db; Password={Helper.GlobalString}"), bsonMapper);
        }

        public ILiteCollection<T> GetCollection<T>(string name)
        {
            return database.GetCollection<T>(name);
        }

        public void Confirm()
        {
            database.Checkpoint();
        }
    }

}