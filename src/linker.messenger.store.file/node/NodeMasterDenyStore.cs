using linker.libs;
using linker.messenger.node;
using LiteDB;
using System.Net;
using IPNetwork = System.Net.IPNetwork;

namespace linker.messenger.store.file.node
{
    /// <summary>
    /// 节点主机禁用
    /// </summary>
    public class NodeMasterDenyStore : INodeMasterDenyStore
    {
        public virtual string StoreName => "relay";

        protected readonly ILiteCollection<MasterDenyStoreInfo> liteCollection;
        public NodeMasterDenyStore(Storefactory storefactory)
        {
            liteCollection = storefactory.GetCollection<MasterDenyStoreInfo>($"{StoreName}_server_master_deny");
        }

        public async Task<bool> Add(MasterDenyAddInfo info)
        {
            IPNetwork net = info.Str.AsSpan().IndexOf('/') < 0 ? new IPNetwork(IPAddress.Parse(info.Str), 32) : IPNetwork.Parse(info.Str);

            uint ip = NetworkHelper.ToValue(net.BaseAddress);
            uint prefixValue = NetworkHelper.ToPrefixValue((byte)net.PrefixLength);
            uint network = NetworkHelper.ToNetworkValue(ip, prefixValue);
            uint broadcast = NetworkHelper.ToBroadcastValue(ip, prefixValue);
            liteCollection.Insert(new MasterDenyStoreInfo
            {
                Str = info.Str,
                Ip = ip,
                Plus = broadcast - network,
                Remark = info.Remark,
            });
            return true;
        }

        public async Task<bool> Delete(MasterDenyDelInfo info)
        {
            return liteCollection.Delete(info.Id);
        }

        public async Task<MasterDenyStoreResponseInfo> Get(MasterDenyStoreRequestInfo info)
        {
            var query = liteCollection.FindAll();
            if (string.IsNullOrWhiteSpace(info.Str) == false)
            {
                query = query.Where(c => (string.IsNullOrWhiteSpace(c.Str) == false && c.Str.Contains(info.Str)) || string.IsNullOrWhiteSpace(c.Remark) == false && c.Remark.Contains(info.Str));
            }
            return new MasterDenyStoreResponseInfo
            {
                Page = info.Page,
                Size = info.Size,
                Count = query.Count(),
                List = query.Skip((info.Page - 1) * info.Size).Take(info.Size).ToList()
            };
        }

        public async Task<bool> Get(uint ip, int plus)
        {
            return liteCollection.Find(c => ip >= c.Ip && ip <= c.Ip + c.Plus).Any();
        }
    }
}
