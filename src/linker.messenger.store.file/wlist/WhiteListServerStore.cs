using LiteDB;
using linker.messenger.wlist;

namespace linker.messenger.store.file.wlist
{
    public sealed class WhiteListServerStore : IWhiteListServerStore
    {
        public WhiteListConfigServerInfo Config => fileConfig.Data.Server.WhiteList;

        private readonly ILiteCollection<WhiteListInfo> liteCollection;
        private readonly FileConfig fileConfig;
        public WhiteListServerStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            liteCollection = dBfactory.GetCollection<WhiteListInfo>("whiteList");
            this.fileConfig = fileConfig;
        }

        public async Task<bool> Add(WhiteListInfo info)
        {
            if (info.Id == 0)
            {
                info.AddTime = DateTime.Now;
                liteCollection.Insert(info);
            }
            else
            {
                liteCollection.UpdateMany(p => new WhiteListInfo
                {
                    Name = info.Name,
                    Remark = info.Remark,
                    Nodes = info.Nodes,
                    Bandwidth = info.Bandwidth,
                    UseTime = info.UseTime,
                    EndTime = info.EndTime,
                    UserId = info.UserId,
                    MachineId = info.MachineId
                }, c => c.Id == info.Id);
            }
            return await Task.FromResult(true).ConfigureAwait(false);
        }
        public async Task<bool> Del(int id)
        {
            return await Task.FromResult(liteCollection.Delete(id)).ConfigureAwait(false);
        }
        public async Task<List<WhiteListInfo>> Get(string type, List<string> userids, List<string> machineIds)
        {
            if (string.IsNullOrWhiteSpace(type)) return [];

            return await Task.FromResult(liteCollection.Find(c => c.Type == type && c.UseTime <= DateTime.Now && c.EndTime > DateTime.Now && (userids.Contains(c.UserId) || machineIds.Contains(c.MachineId))).ToList()).ConfigureAwait(false);
        }
        public async Task<WhiteListInfo> Get(string tradeNo)
        {
            return await Task.FromResult(liteCollection.FindOne(c => c.TradeNo == tradeNo)).ConfigureAwait(false);
        }
        public async Task<WhiteListPageResultInfo> Page(WhiteListPageRequestInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Type))
            {
                return new WhiteListPageResultInfo
                {
                    Page = info.Page,
                    Size = info.Size,
                    Count = 0,
                    List = []
                };
            }

            ILiteQueryable<WhiteListInfo> query = liteCollection.Query().Where(c => c.Type == info.Type);
            if (string.IsNullOrWhiteSpace(info.MachineId) == false)
            {
                query = query.Where(x => x.MachineId == info.MachineId);
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

            return await Task.FromResult(new WhiteListPageResultInfo
            {
                Page = info.Page,
                Size = info.Size,
                Count = query.Count(),
                List = query.Skip((info.Page - 1) * info.Size).Limit(info.Size).ToList()
            }).ConfigureAwait(false);
        }

    }
}
