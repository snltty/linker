using linker.messenger.relay.server;
using LiteDB;
using Yitter.IdGenerator;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerCdkeyStore : IRelayServerCdkeyStore
    {
        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<RelayServerCdkeyStoreInfo> liteCollection;
        public RelayServerCdkeyStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<RelayServerCdkeyStoreInfo>("relayCdkey");
        }

        public async Task<bool> Add(RelayServerCdkeyStoreInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Id))
            {
                info.Id = ObjectId.NewObjectId().ToString();
                info.CdKey = Guid.NewGuid().ToString().ToUpper();
                info.AddTime = DateTime.Now;
                info.UseTime = DateTime.Now;
                info.LastBytes = info.MaxBytes;
                info.CdkeyId = YitIdHelper.NextId();
                liteCollection.Insert(info);
            }
            else
            {
                liteCollection.Update(info);
            }
            return await Task.FromResult(true);
        }
        public async Task<bool> Del(long id)
        {
            return await Task.FromResult(liteCollection.DeleteMany(c => c.CdkeyId == id) > 0);
        }

        public async Task<bool> Traffic(Dictionary<long, long> dic)
        {
            foreach (var item in dic)
            {
                var info = liteCollection.FindOne(x => x.CdkeyId == item.Key);
                if (info != null)
                {
                    long bytes = info.LastBytes >= item.Value ? info.LastBytes - item.Value : 0;
                    liteCollection.UpdateMany(x => new RelayServerCdkeyStoreInfo { LastBytes = bytes, UseTime = DateTime.Now }, c => c.CdkeyId == item.Key);
                }
            }
            return await Task.FromResult(true);
        }

        public async Task<List<RelayServerCdkeyStoreInfo>> GetAvailable(string userid)
        {
            return await Task.FromResult(liteCollection.Find(x => x.UserId == userid && x.LastBytes > 0 && x.StartTime <= DateTime.Now && x.EndTime < DateTime.Now).ToList());
        }
        public async Task<List<RelayServerCdkeyStoreInfo>> Get(List<long> ids)
        {
            return await Task.FromResult(liteCollection.Find(x => ids.Contains(x.CdkeyId)).ToList());
        }

        public async Task<RelayServerCdkeyPageResultInfo> Get(RelayServerCdkeyPageRequestInfo info)
        {
            ILiteQueryable<RelayServerCdkeyStoreInfo> query = liteCollection.Query();

            if (string.IsNullOrWhiteSpace(info.Order) == false)
            {
                query = query.OrderBy(info.Order, info.Sort == "asc" ? Query.Ascending : Query.Descending);
            }
            else
            {
                query = query.OrderBy(c => c.CdkeyId, Query.Descending);
            }
            if (string.IsNullOrWhiteSpace(info.UserId) == false)
            {
                query = query.Where(x => x.UserId == info.UserId);
            }
            if (string.IsNullOrWhiteSpace(info.Remark) == false)
            {
                query = query.Where(x => x.Remark.Contains(info.Remark));
            }

            return await Task.FromResult(new RelayServerCdkeyPageResultInfo
            {
                Page = info.Page,
                Size = info.Size,
                Count = query.Count(),
                List = query.Skip((info.Page - 1) * info.Size).Limit(info.Size).ToList()
            });
        }


    }
}
