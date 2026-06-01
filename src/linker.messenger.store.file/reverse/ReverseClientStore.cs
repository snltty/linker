using linker.libs;
using linker.libs.extends;
using linker.messenger.reverse;
using linker.messenger.reverse.client;
using LiteDB;

namespace linker.messenger.store.file.reverse
{
    public sealed class ReverseClientStore : IReverseClientStore
    {
        private readonly ILiteCollection<ReverseInfo> liteCollection;

        public ReverseClientStore(Storefactory dBfactory)
        {
            liteCollection = dBfactory.GetCollection<ReverseInfo>("sforward");
            liteCollection.UpdateMany(c => new ReverseInfo { Started = false }, c => c.Started == true);
        }
        public int Count()
        {
            return liteCollection.Count();
        }

        public IEnumerable<ReverseInfo> Get()
        {
            return liteCollection.FindAll().ToList();
        }

        public ReverseInfo Get(long id)
        {
            return liteCollection.FindOne(x => x.Id == id);
        }

        public ReverseInfo Get(string domain)
        {
            return liteCollection.FindOne(x => x.Domain == domain);
        }

        public ReverseInfo GetPort(int port)
        {
            return liteCollection.FindOne(c => c.RemotePort == port);
        }
        public bool Add(ReverseInfo info)
        {
            ReverseInfo old = liteCollection.FindOne(c => info.RemotePort > 0 && c.RemotePort == info.RemotePort || string.IsNullOrWhiteSpace(info.Domain) == false && c.Domain == info.Domain);
            if (old != null && old.Id != info.Id)
            {
                return false;
            }

            if (PortRange(info.Domain, out int min, out int max))
            {
                info.RemotePortMin = min;
                info.RemotePortMax = max;
            }
            if (info.Id != 0)
            {
                liteCollection.UpdateMany(c => new ReverseInfo
                {
                    RemotePort = info.RemotePort,
                    Name = info.Name,
                    LocalEP = info.LocalEP,
                    Domain = info.Domain,
                    Started = info.Started,
                    RemotePortMin = info.RemotePortMin,
                    RemotePortMax = info.RemotePortMax,
                    NodeId = info.NodeId
                }, c => c.Id == info.Id);
            }
            else
            {

                liteCollection.Insert(info);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"add reverse {info.ToJson()}");
            }
            return true;
        }
        public bool Update(long id, bool started, string msg)
        {
            return liteCollection.UpdateMany(c => new ReverseInfo { Started = started, Msg = msg }, c => c.Id == id) > 0;
        }
        public bool Update(long id, bool started)
        {
            return liteCollection.UpdateMany(c => new ReverseInfo { Started = started }, c => c.Id == id) > 0;
        }
        public bool Update(long id, string localMsg)
        {
            return liteCollection.UpdateMany(c => new ReverseInfo { LocalMsg = localMsg }, c => c.Id == id) > 0;
        }

        public bool UpdateNodeId1(long id, string nodeid1)
        {
            return liteCollection.UpdateMany(c => new ReverseInfo { NodeId1 = nodeid1 }, c => c.Id == id) > 0;
        }

        public bool Remove(long id)
        {
            return liteCollection.Delete(id);
        }

        private bool PortRange(string str, out int min, out int max)
        {
            min = 0; max = 0;

            if (string.IsNullOrWhiteSpace(str)) return false;

            string[] arr = str.Split('/');
            return arr.Length == 2 && int.TryParse(arr[0], out min) && int.TryParse(arr[1], out max);
        }

    }
}
