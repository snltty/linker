using System;
using System.Collections.Generic;

namespace linker.discovery
{
    public sealed class DiscoveryProtocolRecentPacketCache
    {
        private readonly object _gate = new();
        private readonly TimeSpan _ttl;
        private readonly Dictionary<ulong, long> _items = [];
        private int _operations;

        public DiscoveryProtocolRecentPacketCache(TimeSpan ttl)
        {
            _ttl = ttl;
        }

        public void Add(ulong hash)
        {
            long expiresAt = DateTime.UtcNow.Add(_ttl).Ticks;
            lock (_gate)
            {
                _items[hash] = expiresAt;
                PruneIfNeeded();
            }
        }

        public bool Contains(ulong hash)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            lock (_gate)
            {
                return _items.TryGetValue(hash, out long expiresAt) && expiresAt > nowTicks;
            }
        }

        private void PruneIfNeeded()
        {
            _operations++;
            if ((_operations & 0x1f) != 0)
            {
                return;
            }

            long nowTicks = DateTime.UtcNow.Ticks;
            List<ulong>? expired = null;
            foreach ((ulong hash, long expiresAt) in _items)
            {
                if (expiresAt <= nowTicks)
                {
                    expired ??= [];
                    expired.Add(hash);
                }
            }

            if (expired is null)
            {
                return;
            }

            foreach (ulong hash in expired)
            {
                _items.Remove(hash);
            }
        }
    }
}
