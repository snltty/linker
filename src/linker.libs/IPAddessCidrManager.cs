using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace linker.libs
{
    public sealed class IPAddessCidrManager<T>
    {
        private readonly ConcurrentDictionary<uint, T> ip2value = new ConcurrentDictionary<uint, T>();
        private HashSet<uint> masks = new HashSet<uint>();
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
            ip2value.AddOrUpdate(network, item.Value, (a, b) => item.Value);
            masks.Add(maskValue);
        }
        public void Delete(T value, Func<T, T, bool> compare)
        {
            foreach (var item in ip2value.Where(c => compare(c.Value, value)).ToList())
            {
                ip2value.TryRemove(item.Key, out _);
            }
        }

        public void Clear()
        {
            ip2value.Clear();
        }

        public bool FindValue(IPAddress ip, out T value)
        {
            return FindValue(NetworkHelper.ToValue(ip), out value);
        }

        public bool FindValue(uint ip, out T value)
        {
            if (ip2value.TryGetValue(ip, out value))
            {
                return true;
            }
            foreach (var item in masks)
            {
                uint network = ip & item;
                if (ip2value.TryGetValue(network, out value))
                {
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
