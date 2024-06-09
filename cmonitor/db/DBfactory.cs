using common.libs.extends;
using LiteDB;
using System.Net;

namespace cmonitor.db
{
    public sealed class DBfactory
    {
        LiteDatabase database;
        public DBfactory()
        {
            BsonMapper bsonMapper = new BsonMapper();
            bsonMapper.RegisterType<IPEndPoint>(serialize: (a) => a.ToString(), deserialize: (a) => IPEndPoint.Parse(a.AsString));
            bsonMapper.RegisterType<IPAddress>(serialize: (a) => a.ToString(), deserialize: (a) => IPAddress.Parse(a.AsString));
            bsonMapper.RegisterType<IPAddress[]>(serialize: (a) => a.ToJson(), deserialize: (a) => a.AsString.DeJson<IPAddress[]>());
            database = new LiteDatabase(@"./configs/db.db", bsonMapper);

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
