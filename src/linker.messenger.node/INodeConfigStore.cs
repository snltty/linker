namespace linker.messenger.node
{
    public interface INodeConfigBase
    {
        public string NodeId { get; set; }
        public string MasterKey { get; set; }
        public string ShareKey { get; set; }
        public string ShareKeyManager { get; set; }
        public string Host { get; set; }
        public string Name { get; set; }

        public int Connections { get; set; }
        public int Bandwidth { get; set; }
        public int DataEachMonth { get; set; }
        public long DataRemain { get; set; }
        public int DataMonth { get; set; }

        public string Url { get; set; }
        public string Logo { get; set; }
    }

    public interface INodeConfigStore<T> where T : class, INodeConfigBase, new()
    {
        public int ServicePort { get; }

        /// <summary>
        /// 节点信息
        /// </summary>
        public T Config { get; }

        /// <summary>
        /// 设置月份
        /// </summary>
        /// <param name="month"></param>
        public void SetDataMonth(int month);
        /// <summary>
        /// 设置剩余流量
        /// </summary>
        /// <param name="value"></param>
        public void SetDataRemain(long value);

        public void SetShareKey(string shareKey);
        public void SetShareKeyManager(string shareKeyManager);

        /// <summary>
        /// 提交保存
        /// </summary>
        public void Confirm();
    }


    public sealed class NodeShareInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MasterKey { get; set; } = string.Empty;

    }
}
