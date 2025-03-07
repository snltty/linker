using linker.libs;
using linker.libs.extends;
using linker.messenger.relay.server;
using LiteDB;
using System.Text;
using System.Text.RegularExpressions;
using Yitter.IdGenerator;

namespace linker.messenger.store.file.relay
{
    public sealed class RelayServerCdkeyStore : IRelayServerCdkeyStore
    {
        private string regex = @"([0-9]+)-([0-9]+)-([0-9]+)\s+([0-9]+):([0-9]+):([0-9]+)";

        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<RelayServerCdkeyStoreInfo> liteCollection;
        private readonly ICrypto crypto;
        public RelayServerCdkeyStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<RelayServerCdkeyStoreInfo>("relayCdkey");
            this.crypto = CryptoFactory.CreateSymmetric(fileConfig.Data.Server.Relay.Cdkey.SecretKey, System.Security.Cryptography.PaddingMode.PKCS7);
        }

        public async Task<bool> Add(RelayServerCdkeyStoreInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Id))
            {
                info.Id = ObjectId.NewObjectId().ToString();
                info.AddTime = DateTime.Now;
                info.UseTime = DateTime.Now;
                info.LastBytes = info.MaxBytes;
                info.CdkeyId = YitIdHelper.NextId();
                info.OrderId = $"Linker{YitIdHelper.NextId()}";
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
            return await Task.FromResult(liteCollection.UpdateMany(c => new RelayServerCdkeyStoreInfo { Deleted = true }, c => c.CdkeyId == id) > 0);
        }
        public async Task<bool> Del(long id, string userid)
        {
            return await Task.FromResult(liteCollection.UpdateMany(c => new RelayServerCdkeyStoreInfo { Deleted = true }, c => c.CdkeyId == id && c.UserId == userid) > 0);
        }

        public async Task<RelayServerCdkeyTestResultInfo> Test(RelayServerCdkeyImportInfo info)
        {
            List<string> error = new List<string>();
            RelayServerCdkeyTestResultInfo result = new RelayServerCdkeyTestResultInfo();

            try
            {
                result.Cdkey = Encoding.UTF8.GetString(crypto.Decode(Convert.FromBase64String(info.Base64)).Span);
                RelayServerCdkeyOrderInfo order = result.Cdkey.DeJson<RelayServerCdkeyOrderInfo>();
                result.Order = order;

                if (order.WidgetUserId != info.UserId)
                {
                    error.Add("UserId");
                }
                if (order.Speed <= 0)
                {
                    error.Add("Speed");
                }
                if (order.GB <= 0)
                {
                    error.Add("GB");
                }
                if (order.Count <= 0)
                {
                    error.Add("Count");
                }
                if (Regex.IsMatch(order.Time, regex) == false)
                {
                    error.Add("Time");
                }
                if (string.IsNullOrWhiteSpace(order.OrderId))
                {
                    error.Add("OrderId");
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                error.Add("Parse");
            }
            result.Field = error;

            return await Task.FromResult(result);
        }
        public async Task<bool> Import(RelayServerCdkeyImportInfo info)
        {
            RelayServerCdkeyTestResultInfo test = await Test(info);
            if (test.Field.Count > 0)
            {
                return false;
            }
            RelayServerCdkeyOrderInfo order = test.Order;
            var time = Regex.Match(order.Time, regex).Groups;
            for (int i = 0; i < order.Count; i++)
            {
                RelayServerCdkeyStoreInfo store = new RelayServerCdkeyStoreInfo
                {
                    UseTime = DateTime.Now,
                    AddTime = DateTime.Now,
                    Bandwidth = order.Speed,
                    CostPrice = order.CostPrice,
                    EndTime = DateTime.Now
                    .AddYears(int.Parse(time[1].Value))
                    .AddMonths(int.Parse(time[2].Value))
                    .AddDays(int.Parse(time[3].Value))
                    .AddHours(int.Parse(time[4].Value))
                    .AddMinutes(int.Parse(time[5].Value))
                    .AddSeconds(int.Parse(time[6].Value)),
                    LastBytes = order.Speed * 1024 * 1024 * 1024,
                    MaxBytes = order.Speed * 1024 * 1024 * 1024,
                    Price = order.Price,
                    Remark = "order",
                    StartTime = DateTime.Now,
                    UserId = order.WidgetUserId,
                    CdkeyId = YitIdHelper.NextId(),
                    Contact = order.Contact,
                    OrderId = order.OrderId,
                    PayPrice = order.PayPrice,
                    UserPrice = order.UserPrice,
                    Id = ObjectId.NewObjectId().ToString()
                };
                liteCollection.Insert(store);
            }
            return await Task.FromResult(true);
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
            return await Task.FromResult(liteCollection.Find(x => x.UserId == userid && x.LastBytes > 0 && x.StartTime <= DateTime.Now && x.EndTime < DateTime.Now && x.Deleted == false).ToList());
        }
        public async Task<List<RelayServerCdkeyStoreInfo>> Get(List<long> ids)
        {
            return await Task.FromResult(liteCollection.Find(x => ids.Contains(x.CdkeyId)).ToList());
        }

        public async Task<RelayServerCdkeyPageResultInfo> Page(RelayServerCdkeyPageRequestInfo info)
        {
            ILiteQueryable<RelayServerCdkeyStoreInfo> query = liteCollection.Query();

            if (info.Flag.HasFlag(RelayServerCdkeyPageRequestFlag.TimeIn))
            {
                query = query.Where(x => x.EndTime > DateTime.Now);
            }
            if (info.Flag.HasFlag(RelayServerCdkeyPageRequestFlag.TimeOut))
            {
                query = query.Where(x => x.EndTime < DateTime.Now);
            }
            if (info.Flag.HasFlag(RelayServerCdkeyPageRequestFlag.BytesIn))
            {
                query = query.Where(x => x.LastBytes > 0);
            }
            if (info.Flag.HasFlag(RelayServerCdkeyPageRequestFlag.BytesOut))
            {
                query = query.Where(x => x.LastBytes <= 0);
            }
            if (info.Flag.HasFlag(RelayServerCdkeyPageRequestFlag.Deleted))
            {
                query = query.Where(x => x.Deleted == true);
            }
            if (info.Flag.HasFlag(RelayServerCdkeyPageRequestFlag.UnDeleted))
            {
                query = query.Where(x => x.Deleted == false);
            }

            if (string.IsNullOrWhiteSpace(info.UserId) == false)
            {
                query = query.Where(x => x.UserId == info.UserId);
            }
            if (string.IsNullOrWhiteSpace(info.Remark) == false)
            {
                query = query.Where(x => x.Remark.Contains(info.Remark));
            }
            if (string.IsNullOrWhiteSpace(info.OrderId) == false)
            {
                query = query.Where(x => x.OrderId.Contains(info.OrderId));
            }
            if (string.IsNullOrWhiteSpace(info.Contact) == false)
            {
                query = query.Where(x => x.Contact.Contains(info.Contact));
            }
            if (string.IsNullOrWhiteSpace(info.Order) == false)
            {
                query = query.OrderBy(info.Order, info.Sort == "asc" ? Query.Ascending : Query.Descending);
            }
            else
            {
                query = query.OrderBy(c => c.CdkeyId, Query.Descending);
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
