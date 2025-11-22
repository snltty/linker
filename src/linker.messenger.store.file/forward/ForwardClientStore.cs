using linker.messenger.forward;
using LiteDB;
using System.Net;

namespace linker.messenger.store.file.forward
{
    public sealed class ForwardClientStore : IForwardClientStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<ForwardInfo> liteCollection;
        public ForwardClientStore(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<ForwardInfo>("forward");
            liteCollection.UpdateMany(c => new ForwardInfo { Proxy = false }, c => c.Proxy == true);
        }
        public int Count()
        {
            return liteCollection.Count();
        }


        public IEnumerable<ForwardInfo> Get()
        {
            return liteCollection.FindAll().ToList();
        }

        public ForwardInfo Get(long id)
        {
            return liteCollection.FindOne(x => x.Id == id);
        }

        public IEnumerable<ForwardInfo> Get(string groupid)
        {
            return liteCollection.Find(x => x.GroupId == groupid).ToList();
        }

        public bool Add(ForwardInfo info)
        {
            //同名或者同端口，但是ID不一样
            ForwardInfo old = liteCollection.FindOne(c => (c.Port == info.Port && c.Port != 0) && c.GroupId == info.GroupId && c.MachineId == info.MachineId);
            if (old != null && old.Id != info.Id) return false;
            if (info.Id != 0)
            {
                liteCollection.UpdateMany(c => new ForwardInfo
                {
                    BindIPAddress = info.BindIPAddress,
                    Port = info.Port,
                    Name = info.Name,
                    TargetEP = info.TargetEP,
                    MachineId = info.MachineId,
                    MachineName = info.MachineName,
                    Started = info.Started,
                    BufferSize = info.BufferSize,
                    GroupId = info.GroupId,
                }, c => c.Id == info.Id);
            }
            else
            {
                liteCollection.Insert(info);
            }

            return true;
        }
        public bool Update(long id, bool started, bool proxy, string msg)
        {
            return liteCollection.UpdateMany(c => new ForwardInfo { Started = started, Proxy = proxy, Msg = msg }, c => c.Id == id) > 0;
        }
        public bool Update(long id, bool started)
        {
            return liteCollection.UpdateMany(c => new ForwardInfo { Started = started }, c => c.Id == id) > 0;
        }

        public bool Update(long id, string msg)
        {
            return liteCollection.UpdateMany(c => new ForwardInfo { Msg = msg }, c => c.Id == id) > 0;
        }
        public bool Update(string machineId, IPEndPoint target, string targetMsg)
        {
            return liteCollection.UpdateMany(c => new ForwardInfo { TargetMsg = targetMsg }, c => c.MachineId == machineId && c.TargetEP == target) > 0;
        }

        public bool Update(long id, int port)
        {
            return liteCollection.UpdateMany(c => new ForwardInfo { Port = port }, c => c.Id == id) > 0;
        }
        public bool Remove(long id)
        {
            return liteCollection.Delete(id);
        }


    }
}
