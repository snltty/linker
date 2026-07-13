using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace linker.discovery
{
    public sealed class DiscoveryRelayQueryTracker
    {
        private readonly object _gate = new();
        private readonly TimeSpan _keyTtl;
        private readonly TimeSpan _recentTtl;
        private readonly Dictionary<string, List<RequesterEntry>> _byKey = new(StringComparer.Ordinal);
        private readonly List<RequesterEntry> _recent = [];
        private int _operations;

        public DiscoveryRelayQueryTracker(TimeSpan keyTtl, TimeSpan recentTtl)
        {
            _keyTtl = keyTtl;
            _recentTtl = recentTtl;
        }

        public void Remember(IReadOnlyList<string> keys, IPEndPoint endpoint)
        {
            DateTime now = DateTime.UtcNow;
            long keyExpiresAt = now.Add(_keyTtl).Ticks;
            long recentExpiresAt = now.Add(_recentTtl).Ticks;
            var copy = new IPEndPoint(endpoint.Address, endpoint.Port);

            lock (_gate)
            {
                PruneIfNeeded();
                AddOrUpdate(_recent, copy, recentExpiresAt);

                foreach (string key in keys)
                {
                    if (!_byKey.TryGetValue(key, out List<RequesterEntry>? entries))
                    {
                        entries = [];
                        _byKey.Add(key, entries);
                    }

                    AddOrUpdate(entries, copy, keyExpiresAt);
                }
            }
        }

        public void GetDestinations(IReadOnlyList<string> keys, List<IPEndPoint> destinations)
        {
            long nowTicks = DateTime.UtcNow.Ticks;

            lock (_gate)
            {
                foreach (string key in keys)
                {
                    if (!_byKey.TryGetValue(key, out List<RequesterEntry>? entries))
                    {
                        continue;
                    }

                    foreach (RequesterEntry entry in entries)
                    {
                        if (entry.ExpiresAtTicks > nowTicks)
                        {
                            AddDistinct(destinations, entry.Endpoint);
                        }
                    }
                }
            }
        }

        public void GetRecentDestinations(List<IPEndPoint> destinations)
        {
            long nowTicks = DateTime.UtcNow.Ticks;

            lock (_gate)
            {
                foreach (RequesterEntry entry in _recent)
                {
                    if (entry.ExpiresAtTicks > nowTicks)
                    {
                        AddDistinct(destinations, entry.Endpoint);
                    }
                }
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
            PruneList(_recent, nowTicks);

            List<string>? emptyKeys = null;
            foreach ((string key, List<RequesterEntry> entries) in _byKey)
            {
                PruneList(entries, nowTicks);
                if (entries.Count == 0)
                {
                    emptyKeys ??= [];
                    emptyKeys.Add(key);
                }
            }

            if (emptyKeys is null)
            {
                return;
            }

            foreach (string key in emptyKeys)
            {
                _byKey.Remove(key);
            }
        }

        private static void PruneList(List<RequesterEntry> entries, long nowTicks)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].ExpiresAtTicks <= nowTicks)
                {
                    entries.RemoveAt(i);
                }
            }
        }

        private static void AddOrUpdate(List<RequesterEntry> entries, IPEndPoint endpoint, long expiresAt)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (SameEndpoint(entries[i].Endpoint, endpoint))
                {
                    entries[i] = new RequesterEntry(endpoint, expiresAt);
                    return;
                }
            }

            entries.Add(new RequesterEntry(endpoint, expiresAt));
        }

        private static void AddDistinct(List<IPEndPoint> destinations, IPEndPoint endpoint)
        {
            foreach (IPEndPoint destination in destinations)
            {
                if (SameEndpoint(destination, endpoint))
                {
                    return;
                }
            }

            destinations.Add(endpoint);
        }

        private static bool SameEndpoint(IPEndPoint left, IPEndPoint right)
        {
            return left.Port == right.Port && left.Address.Equals(right.Address);
        }

        private readonly record struct RequesterEntry(IPEndPoint Endpoint, long ExpiresAtTicks);
    }
}
