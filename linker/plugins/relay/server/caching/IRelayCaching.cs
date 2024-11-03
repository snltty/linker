namespace linker.plugins.relay.server.caching
{
    public interface IRelayCaching
    {
        public string Name { get; }

        public bool TryAdd<T>(string key, T value, int expired);
        public bool TryGetValue<T>(string key, out T value);
    }

}
