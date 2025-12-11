using System.Net;

namespace linker.messenger.node
{
    /// <summary>
    /// 禁用的主机
    /// </summary>
    public interface INodeMasterDenyStore
    {
        public Task<MasterDenyStoreResponseInfo> Get(MasterDenyStoreRequestInfo request);
        public Task<bool> Get(uint ip,int plus);
        public Task<bool> Add(MasterDenyAddInfo info);
        public Task<bool> Delete(MasterDenyDelInfo info);
    }

    public sealed class MastersRequestInfo
    {
        public string NodeId { get; set; }
        public int Page { get; set; }
        public int Sise { get; set; }
    }
    public sealed class MastersResponseInfo
    {
        public int Page { get; set; }
        public int Sise { get; set; }
        public int Count { get; set; }
        public List<MasterConnInfo> List { get; set; } = new List<MasterConnInfo>();
    }
    public sealed class MasterConnInfo
    {
        public IPEndPoint Addr { get; set; }
        public string NodeId { get; set; }
    }


    public sealed class MasterDenyStoreRequestInfo
    {
        public string NodeId { get; set; }
        public int Page { get; set; }
        public int Sise { get; set; }
        public string Str { get; set; }
    }
    public sealed class MasterDenyStoreResponseInfo
    {
        public int Page { get; set; }
        public int Sise { get; set; }
        public int Count { get; set; }
        public List<MasterDenyStoreInfo> List { get; set; } = new List<MasterDenyStoreInfo>();
    }
    public sealed class MasterDenyStoreInfo
    {
        public int Id { get; set; }
        public uint Ip { get; set; }
        public uint Plus { get; set; }
        public string Str { get; set; }
        public string Remark { get; set; }
    }


    public sealed class MasterDenyAddInfo
    {
        public string NodeId { get; set; }
        public int Id { get; set; }
        public string Str { get; set; }
        public string Remark { get; set; }
    }
    public sealed class MasterDenyDelInfo
    {
        public string NodeId { get; set; }
        public int Id { get; set; }
    }
}
