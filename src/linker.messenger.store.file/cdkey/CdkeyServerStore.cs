using linker.libs;
using linker.libs.extends;
using linker.messenger.cdkey;
using LiteDB;
using System.Text;
using System.Text.RegularExpressions;

namespace linker.messenger.store.file.cdkey
{
    public sealed class CdkeyServerStore : ICdkeyServerStore
    {
        private readonly string regex = @"([0-9]+|\?)-([0-9]+|\?)-([0-9]+|\?)\s+([0-9]+|\?):([0-9]+|\?):([0-9]+|\?)";
        private int index = 0;

        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<CdkeyStoreInfo> liteCollection;
        private readonly ICrypto crypto;
        private readonly FileConfig fileConfig;
        public CdkeyServerStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<CdkeyStoreInfo>("relayCdkey");
            this.fileConfig = fileConfig;

            this.crypto = CryptoFactory.CreateSymmetric(fileConfig.Data.Server.Cdkey.SecretKey, System.Security.Cryptography.PaddingMode.PKCS7);
        }

        public async Task<bool> Add(CdkeyStoreInfo info)
        {
            if (info.Id == 0)
            {
                Interlocked.Increment(ref index);
                Interlocked.CompareExchange(ref index, 0, 65535);

                info.AddTime = DateTime.Now;
                info.UseTime = DateTime.Now;
                info.StartTime = DateTime.Now;
                info.LastBytes = info.MaxBytes;
                info.OrderId = $"Linker{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{index}";
                liteCollection.Insert(info);
            }
            else
            {
                liteCollection.Update(info);
            }
            return await Task.FromResult(true).ConfigureAwait(false);
        }
        public async Task<bool> Del(int id)
        {
            return await Task.FromResult(liteCollection.UpdateMany(c => new CdkeyStoreInfo { Deleted = true }, c => c.Id == id) > 0).ConfigureAwait(false);
        }
        public async Task<bool> Del(int id, string userid)
        {
            return await Task.FromResult(liteCollection.UpdateMany(c => new CdkeyStoreInfo { Deleted = true }, c => c.Id == id && c.UserId == userid) > 0).ConfigureAwait(false);
        }

        public async Task<CdkeyTestResultInfo> Test(CdkeyImportInfo info)
        {
            List<string> error = new List<string>();
            CdkeyTestResultInfo result = new CdkeyTestResultInfo();

            try
            {
                result.Cdkey = Encoding.UTF8.GetString(crypto.Decode(Convert.FromBase64String(info.Base64)).Span);
                CdkeyOrderInfo order = result.Cdkey.DeJson<CdkeyOrderInfo>();
                result.Order = order;

                if (string.IsNullOrWhiteSpace(order.Type))
                {
                    error.Add("Type");
                }

                if (order.WidgetUserId != info.UserId || string.IsNullOrWhiteSpace(order.WidgetUserId))
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

            return await Task.FromResult(result).ConfigureAwait(false);
        }
        public async Task<string> Import(CdkeyImportInfo info)
        {
            CdkeyTestResultInfo test = await Test(info).ConfigureAwait(false);

            if (test.Field.Count > 0)
            {
                if (test.Field.Contains("Parse"))
                {
                    return "Parse";
                }
                else
                {
                    return "Field";
                }
            }
            if (liteCollection.Count(c => c.OrderId == test.Order.OrderId) > 0)
            {
                return "OrderId";
            }

            CdkeyOrderInfo order = test.Order;
            var time = Regex.Match(order.Time, regex).Groups;
            CdkeyStoreInfo store = new CdkeyStoreInfo
            {
                Type = order.Type,
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
                LastBytes = (long)order.GB * 1024 * 1024 * 1024 * order.Count,
                MaxBytes = (long)order.GB * 1024 * 1024 * 1024 * order.Count,
                Price = order.Price,
                Remark = "order",
                StartTime = DateTime.Now,
                UserId = order.WidgetUserId,
                Contact = order.Contact,
                OrderId = order.OrderId,
                PayPrice = order.PayPrice,
                UserPrice = order.UserPrice,
                Values = order.Values ?? [],
            };
            liteCollection.Insert(store);
            return await Task.FromResult(string.Empty).ConfigureAwait(false);
        }

        public async Task<bool> Traffic(Dictionary<int, long> dic)
        {
            foreach (var item in dic)
            {
                var info = liteCollection.FindOne(x => x.Id == item.Key);
                if (info != null)
                {
                    long bytes = info.LastBytes >= item.Value ? info.LastBytes - item.Value : 0;
                    liteCollection.UpdateMany(x => new CdkeyStoreInfo { LastBytes = bytes, UseTime = DateTime.Now }, c => c.Id == item.Key);
                }
            }
            return await Task.FromResult(true).ConfigureAwait(false);
        }
        public async Task<Dictionary<int, long>> GetLastBytes(List<int> ids)
        {
            return await Task.FromResult(liteCollection.Find(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.LastBytes)).ConfigureAwait(false);
        }

        public async Task<List<CdkeyStoreInfo>> GetAvailable(string userid, string type)
        {
            if (string.IsNullOrWhiteSpace(userid) || string.IsNullOrWhiteSpace(type)) return [];
            return await Task.FromResult(liteCollection.Find(x => x.UserId == userid && x.Type == type && x.LastBytes > 0 && x.StartTime <= DateTime.Now && x.EndTime >= DateTime.Now && x.Deleted == false).ToList()).ConfigureAwait(false);
        }
        public async Task<List<CdkeyStoreInfo>> GetAvailable(string userid, string type, string value)
        {
            if (string.IsNullOrWhiteSpace(userid) || string.IsNullOrWhiteSpace(type)) return [];
            return await Task.FromResult(liteCollection.Find(x => x.UserId == userid && x.Type == type && x.Values.Contains(value) && x.LastBytes > 0 && x.StartTime <= DateTime.Now && x.EndTime >= DateTime.Now && x.Deleted == false).ToList()).ConfigureAwait(false);
        }
        public async Task<List<CdkeyStoreInfo>> Get(List<int> ids)
        {
            return await Task.FromResult(liteCollection.Find(x => ids.Contains(x.Id)).ToList()).ConfigureAwait(false);
        }

        public async Task<CdkeyPageResultInfo> Page(CdkeyPageRequestInfo info)
        {
            ILiteQueryable<CdkeyStoreInfo> query = liteCollection.Query();

            if (info.Flag.HasFlag(CdkeyPageRequestFlag.TimeIn))
            {
                query = query.Where(x => x.StartTime <= DateTime.Now && x.EndTime >= DateTime.Now);
            }
            if (info.Flag.HasFlag(CdkeyPageRequestFlag.TimeOut))
            {
                query = query.Where(x => x.StartTime > DateTime.Now || x.EndTime < DateTime.Now);
            }
            if (info.Flag.HasFlag(CdkeyPageRequestFlag.BytesIn))
            {
                query = query.Where(x => x.LastBytes > 0);
            }
            if (info.Flag.HasFlag(CdkeyPageRequestFlag.BytesOut))
            {
                query = query.Where(x => x.LastBytes <= 0);
            }
            if (info.Flag.HasFlag(CdkeyPageRequestFlag.Deleted))
            {
                query = query.Where(x => x.Deleted == true);
            }
            if (info.Flag.HasFlag(CdkeyPageRequestFlag.UnDeleted))
            {
                query = query.Where(x => x.Deleted == false);
            }

            if (string.IsNullOrWhiteSpace(info.Type) == false)
            {
                query = query.Where(x => x.Type == info.Type);
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
                query = query.OrderBy(c => c.Id, Query.Descending);
            }

            return await Task.FromResult(new CdkeyPageResultInfo
            {
                Page = info.Page,
                Size = info.Size,
                Count = query.Count(),
                List = query.Skip((info.Page - 1) * info.Size).Limit(info.Size).ToList()
            }).ConfigureAwait(false);
        }


    }
}
