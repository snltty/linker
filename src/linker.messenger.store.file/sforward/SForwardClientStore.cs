using linker.libs.extends;
using linker.libs;
using linker.messenger.sforward;
using linker.messenger.sforward.client;
using LiteDB;
using linker.messenger.forward;

namespace linker.messenger.store.file.sforward
{
    public sealed class SForwardClientStore : ISForwardClientStore
    {
        public string SecretKey => fileConfig.Data.Client.SForward.SecretKey;

        private readonly FileConfig fileConfig;
        private readonly RunningConfig runningConfig;
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<SForwardInfo> liteCollection;

        public SForwardClientStore(FileConfig fileConfig, RunningConfig runningConfig, Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<SForwardInfo>("sforward");

            this.fileConfig = fileConfig;
            this.runningConfig = runningConfig;
            foreach (var item in runningConfig.Data.SForwards)
            {
                item.Proxy = false;
                item.Id = 0;
                liteCollection.Insert(item);
            }
            runningConfig.Data.SForwards = new List<SForwardInfo>();
            runningConfig.Data.Update();

            liteCollection.UpdateMany(c => new SForwardInfo { Proxy = false }, c => c.Proxy == true);
        }
        public bool SetSecretKey(string key)
        {
            fileConfig.Data.Client.SForward.SecretKey = key;
            fileConfig.Data.Update();
            return true;
        }

        public int Count()
        {
            return liteCollection.Count();
        }

        public IEnumerable<SForwardInfo> Get()
        {
            return liteCollection.FindAll();
        }

        public SForwardInfo Get(int id)
        {
            return liteCollection.FindOne(x => x.Id == id);
        }

        public SForwardInfo Get(string domain)
        {
            return liteCollection.FindOne(x => x.Domain == domain);
        }

        public SForwardInfo GetPort(int port)
        {
            return liteCollection.FindOne(c => c.RemotePort == port);
        }
        public bool Add(SForwardInfo info)
        {
            SForwardInfo old = liteCollection.FindOne(c => info.RemotePort > 0 && c.RemotePort == info.RemotePort || string.IsNullOrWhiteSpace(info.Domain) == false && c.Domain == info.Domain);
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
                liteCollection.UpdateMany(c => new SForwardInfo
                {
                    RemotePort = info.RemotePort,
                    Name = info.Name,
                    LocalEP = info.LocalEP,
                    Domain = info.Domain,
                    Started = info.Started,
                    RemotePortMin = info.RemotePortMin,
                    RemotePortMax = info.RemotePortMax
                }, c => c.Id == info.Id);
            }
            else
            {

                liteCollection.Insert(info);
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Info($"add sforward {info.ToJson()}");
            }
            return true;
        }
        public bool Update(int id, bool started, string msg)
        {
            return liteCollection.UpdateMany(c => new SForwardInfo { Started = started, Msg = msg }, c => c.Id == id) > 0;
        }
        public bool Update(int id, bool started, bool proxy, string msg)
        {
            return liteCollection.UpdateMany(c => new SForwardInfo { Started = started, Proxy = proxy, Msg = msg }, c => c.Id == id) > 0;
        }
        public bool Update(int id, bool started)
        {
            return liteCollection.UpdateMany(c => new SForwardInfo { Started = started }, c => c.Id == id) > 0;
        }
        public bool Update(int id, string localMsg)
        {
            return liteCollection.UpdateMany(c => new SForwardInfo { LocalMsg = localMsg }, c => c.Id == id) > 0;
        }

        public bool Remove(int id)
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
