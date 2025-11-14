using linker.libs;
using linker.messenger.signin;
using System.Collections.Concurrent;

namespace linker.messenger.decenter
{
    /// <summary>
    /// 计数的分布式数据
    /// </summary>
    public sealed class CounterDecenter : IDecenter
    {
        public string Name => "counter";
        public VersionManager PushVersion { get; } = new VersionManager();
        public VersionManager DataVersion { get; } = new VersionManager();
        public bool Force => CountDic.Count < 2;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, int>> CountDic { get; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();
        private readonly ConcurrentDictionary<string, int> values = new ConcurrentDictionary<string, int>();

        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;
        public CounterDecenter(ISerializer serializer, ISignInClientStore signInClientStore)
        {
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
        }

        public void SetValue(string key, int value)
        {
            values.AddOrUpdate(key, value, (a, b) => value);
            PushVersion.Increment();
        }

        public void Refresh()
        {
            PushVersion.Increment();
        }

        public Memory<byte> GetData()
        {
            List<ValueTuple<string, string, int>> result = values.Select(c => new ValueTuple<string, string, int>(c.Key, signInClientStore.Id, c.Value)).ToList();
            return serializer.Serialize(result);
        }
        public void AddData(Memory<byte> data)
        {
            List<ValueTuple<string, string, int>> info = serializer.Deserialize<List<ValueTuple<string, string, int>>>(data.Span);
            foreach (var item in info)
            {
                Addata(item);
            }
           
        }
        public void AddData(List<ReadOnlyMemory<byte>> data)
        {
            List<List<ValueTuple<string, string, int>>> list = data.Select(c => serializer.Deserialize<List<ValueTuple<string, string, int>>>(c.Span)).ToList();
            foreach (var info in list)
            {
                foreach (var item in info)
                {
                    Addata(item);
                }
            }
        }
        private void Addata(ValueTuple<string, string, int> item)
        {
            if (CountDic.TryGetValue(item.Item1, out ConcurrentDictionary<string, int> value) == false)
            {
                value = new ConcurrentDictionary<string, int>();
                CountDic.TryAdd(item.Item1, value);
            }
            value.AddOrUpdate(item.Item2, item.Item3, (a, b) => item.Item3);
        }

        public void ClearData()
        {
            CountDic.Clear();
        }
        public void ProcData()
        {
        }
    }
}
