using linker.messenger.relay.server;
using LiteDB;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerCdkeyStore : IRelayServerCdkeyStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<RelayServerCdkeyInfo> liteCollection;
        public RelayServerCdkeyStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<RelayServerCdkeyInfo>("relayCdkey");
        }

        public async Task<bool> Add(RelayServerCdkeyInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Id))
            {
                info.Id = ObjectId.NewObjectId().ToString();
                liteCollection.Insert(info);
            }
            else
            {
                liteCollection.Update(info);
            }
            return await Task.FromResult(true);
        }
        public async Task<bool> Del(string id)
        {
            return await Task.FromResult(liteCollection.Delete(id));
        }

        public async Task<List<RelayServerCdkeyInfo>> Get(string userid)
        {
            return await Task.FromResult(liteCollection.Find(x => x.UserId == userid && x.LastBytes > 0 && x.StartTime <= DateTime.Now && x.EndTime < DateTime.Now).ToList());
        }

        public async Task<RelayServerCdkeyPageResultInfo> Get(RelayServerCdkeyPageRequestInfo relayServerCdkeyPageRequestInfo)
        {
            ILiteQueryable<RelayServerCdkeyInfo> query = liteCollection.Query();

            if (string.IsNullOrWhiteSpace(relayServerCdkeyPageRequestInfo.Order) == false)
            {
                query = query.OrderBy(relayServerCdkeyPageRequestInfo.Order, relayServerCdkeyPageRequestInfo.Sort == "asc" ? Query.Ascending : Query.Descending);
            }
            if (string.IsNullOrWhiteSpace(relayServerCdkeyPageRequestInfo.UserId) == false)
            {
                query = query.Where(x => x.UserId == relayServerCdkeyPageRequestInfo.UserId);
            }
            if (string.IsNullOrWhiteSpace(relayServerCdkeyPageRequestInfo.Remark) == false)
            {
                query = query.Where(x => x.Remark.Contains(relayServerCdkeyPageRequestInfo.Remark));
            }

            return await Task.FromResult(new RelayServerCdkeyPageResultInfo
            {
                Page = relayServerCdkeyPageRequestInfo.Page,
                Size = relayServerCdkeyPageRequestInfo.Size,
                Count = query.Count(),
                List = query.Skip((relayServerCdkeyPageRequestInfo.Page - 1) * relayServerCdkeyPageRequestInfo.Size).Limit(relayServerCdkeyPageRequestInfo.Size).ToList()
            });
        }
    }
}
