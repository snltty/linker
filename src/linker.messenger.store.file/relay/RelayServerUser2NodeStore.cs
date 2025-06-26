using linker.messenger.relay.server;
using LiteDB;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerUser2NodeStore : IRelayServerUser2NodeStore
    {
        private readonly ILiteCollection<RelayServerUser2NodeInfo> liteCollection;
        public RelayServerUser2NodeStore(Storefactory dBfactory)
        {
            liteCollection = dBfactory.GetCollection<RelayServerUser2NodeInfo>("relayUser2Node");
        }

        public async Task<bool> Add(RelayServerUser2NodeInfo info)
        {
            if (info.Id == 0)
            {
                info.AddTime = DateTime.Now;
                liteCollection.Insert(info);
            }
            else
            {
                liteCollection.UpdateMany(p => new RelayServerUser2NodeInfo
                {
                    Name = info.Name,
                    Remark = info.Remark,
                    Nodes = info.Nodes,
                }, c => c.Id == info.Id);
            }
            return await Task.FromResult(true).ConfigureAwait(false);
        }
        public async Task<bool> Del(int id)
        {
            return await Task.FromResult(liteCollection.Delete(id)).ConfigureAwait(false);
        }

        public async Task<List<string>> Get(string userid)
        {
            if (string.IsNullOrWhiteSpace(userid)) return [];
            return await Task.FromResult(liteCollection.Find(c => c.UserId == userid).SelectMany(c => c.Nodes).ToList()).ConfigureAwait(false);
        }

        public async Task<RelayServerUser2NodePageResultInfo> Page(RelayServerUser2NodePageRequestInfo info)
        {
            ILiteQueryable<RelayServerUser2NodeInfo> query = liteCollection.Query();
            if (string.IsNullOrWhiteSpace(info.UserId) == false)
            {
                query = query.Where(x => x.UserId == info.UserId);
            }
            if (string.IsNullOrWhiteSpace(info.Name) == false)
            {
                query = query.Where(x => x.Name.Contains(info.Name));
            }
            if (string.IsNullOrWhiteSpace(info.Remark) == false)
            {
                query = query.Where(x => x.Remark.Contains(info.Remark));
            }

            query = query.OrderBy(c => c.Id, Query.Descending);

            return await Task.FromResult(new RelayServerUser2NodePageResultInfo
            {
                Page = info.Page,
                Size = info.Size,
                Count = query.Count(),
                List = query.Skip((info.Page - 1) * info.Size).Limit(info.Size).ToList()
            }).ConfigureAwait(false);
        }


    }
}
