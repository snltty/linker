using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace linker.libs
{
    public sealed class IPAddessCidrManager<T>
    {
        private readonly ConcurrentDictionary<uint, CidrAddInfo<T>> ip2value = new ConcurrentDictionary<uint, CidrAddInfo<T>>();
        private readonly ConcurrentDictionary<string, T> routes = new ConcurrentDictionary<string, T>();
        private HashSet<uint> masks = new HashSet<uint>();

        public Dictionary<string, T> Routes => routes.ToDictionary();

        public void Add(CidrAddInfo<T>[] items)
        {
            foreach (CidrAddInfo<T> item in items)
            {
                Add(item);
            }
        }
        public void Add(CidrAddInfo<T> item)
        {
            uint maskValue = NetworkHelper.ToPrefixValue(item.PrefixLength);
            uint network = item.IPAddress & maskValue;
            ip2value.AddOrUpdate(network, item, (a, b) => item);
            routes.AddOrUpdate($"{NetworkHelper.ToIP(item.IPAddress)}/{item.PrefixLength}", item.Value, (a, b) => item.Value);
            masks.Add(maskValue);
        }
        public void Delete(T value, Func<T, T, bool> compare)
        {
            foreach (var item in ip2value.Where(c => compare(c.Value.Value, value)).ToList())
            {
                ip2value.TryRemove(item.Key, out _);
                routes.TryRemove($"{NetworkHelper.ToIP(item.Value.IPAddress)}/{item.Value.PrefixLength}", out _);
            }
        }

        public void Clear()
        {
            ip2value.Clear();
            routes.Clear();
        }

        public bool FindValue(IPAddress ip, out T value)
        {
            return FindValue(NetworkHelper.ToValue(ip), out value);
        }
        public bool FindValue(uint ip, out T value)
        {
            value = default;
            if (ip2value.TryGetValue(ip, out CidrAddInfo<T> item))
            {
                value = item.Value;
                return true;
            }
            foreach (var mask in masks)
            {
                uint network = ip & mask;
                if (ip2value.TryGetValue(network, out item))
                {
                    value = item.Value;
                    return true;
                }
            }
            return false;
        }
    }

    public sealed class CidrAddInfo<T>
    {
        /// <summary>
        /// ip，存小端
        /// </summary>
        public uint IPAddress { get; set; }
        public byte PrefixLength { get; set; }
        public T Value { get; set; }
    }
}
