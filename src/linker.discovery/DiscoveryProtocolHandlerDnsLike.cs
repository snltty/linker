using System;
using System.Collections.Generic;

namespace linker.discovery
{
    internal static class DiscoveryProtocolHandlerDnsLike
    {
        public static int GetQueryKeys(ReadOnlySpan<byte> payload, ICollection<string> keys, string prefix)
        {
            if (payload.Length < 12)
            {
                return 0;
            }

            int questionCount = DiscoveryProtocolDnsPacket.ReadUInt16(payload, 4);
            int offset = 12;
            int added = 0;

            for (int i = 0; i < questionCount; i++)
            {
                if (!DiscoveryProtocolDnsPacket.TryReadQuestion(payload, ref offset, out string name, out ushort type, out ushort dnsClass))
                {
                    break;
                }

                added += AddKeys(prefix, name, type, dnsClass, keys);
            }

            return added;
        }

        public static int GetResponseKeys(ReadOnlySpan<byte> payload, ICollection<string> keys, string prefix)
        {
            if (payload.Length < 12)
            {
                return 0;
            }

            if ((payload[2] & 0x80) == 0)
            {
                return 0;
            }

            int questionCount = DiscoveryProtocolDnsPacket.ReadUInt16(payload, 4);
            int answerCount = DiscoveryProtocolDnsPacket.ReadUInt16(payload, 6);
            int authorityCount = DiscoveryProtocolDnsPacket.ReadUInt16(payload, 8);
            int additionalCount = DiscoveryProtocolDnsPacket.ReadUInt16(payload, 10);
            int offset = 12;

            for (int i = 0; i < questionCount; i++)
            {
                if (!DiscoveryProtocolDnsPacket.TryReadQuestion(payload, ref offset, out _, out _, out _))
                {
                    return 0;
                }
            }

            int added = 0;
            int recordCount = answerCount + authorityCount + additionalCount;
            for (int i = 0; i < recordCount; i++)
            {
                if (!DiscoveryProtocolDnsPacket.TryReadResourceRecordHeader(
                        payload,
                        ref offset,
                        out string name,
                        out ushort type,
                        out ushort dnsClass,
                        out ushort dataLength))
                {
                    break;
                }

                added += AddKeys(prefix, name, type, dnsClass, keys);
                if (offset + dataLength > payload.Length)
                {
                    break;
                }

                offset += dataLength;
            }

            return added;
        }

        private static int AddKeys(string prefix, string name, ushort type, ushort dnsClass, ICollection<string> keys)
        {
            if (name.Length == 0)
            {
                return 0;
            }

            ushort normalizedClass = (ushort)(dnsClass & 0x7fff);
            int before = keys.Count;
            DiscoveryProtocolKeyHelper.AddDistinct(keys, prefix + ":" + name + ":*");
            DiscoveryProtocolKeyHelper.AddDistinct(keys, prefix + ":" + name + ":" + type + ":" + normalizedClass);
            return keys.Count - before;
        }
    }
}
