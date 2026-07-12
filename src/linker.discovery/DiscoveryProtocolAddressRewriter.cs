using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace linker.discovery;

internal static class DiscoveryProtocolAddressRewriter
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static ReadOnlyMemory<byte> RewriteLanToTun(
        DiscoveryProtocolInfo protocol,
        IDiscoveryProtocolMatcher matcher,
        ReadOnlyMemory<byte> packet,
        IReadOnlyList<DiscoveryAddressMapEntry> maps,
        Action<IPAddress, IPAddress>? addressRewritten,
        Action<string, string, string>? payloadRewritten,
        out bool rewritten)
    {
        rewritten = false;
        bool isWsDiscovery = IsWsDiscoveryProtocol(protocol, matcher);
        if (packet.Length == 0 || (maps.Count == 0 && !isWsDiscovery))
        {
            return packet;
        }

        byte[]? next = null;
        if (IsTextUrlProtocol(protocol, matcher))
        {
            next = RewriteUrlHosts(packet.Span, maps, addressRewritten);
        }
        else if (IsDnsLikeProtocol(protocol, matcher))
        {
            next = RewriteDnsLikeAddresses(protocol, matcher, packet, maps, addressRewritten);
        }

        if (isWsDiscovery)
        {
            RaiseXmlElementRewrite("XAddrs", packet.Span, next ?? packet.Span, payloadRewritten);
        }

        if (next is null)
        {
            return packet;
        }

        rewritten = true;
        return next;
    }

    private static bool IsTextUrlProtocol(DiscoveryProtocolInfo protocol, IDiscoveryProtocolMatcher matcher)
    {
        return protocol.Port is 1900 or 3702 ||
            matcher is DiscoveryProtocolMatcherSsdp or DiscoveryProtocolMatcherWs;
    }

    private static bool IsWsDiscoveryProtocol(DiscoveryProtocolInfo protocol, IDiscoveryProtocolMatcher matcher)
    {
        return protocol.Port == 3702 || matcher is DiscoveryProtocolMatcherWs;
    }

    private static bool IsDnsLikeProtocol(DiscoveryProtocolInfo protocol, IDiscoveryProtocolMatcher matcher)
    {
        return protocol.Port is 137 or 5353 or 5355 ||
            matcher is DiscoveryProtocolMatcherMdns or
                DiscoveryProtocolMatcherLlmnr or
                DiscoveryProtocolMatcherNbns;
    }

    private static byte[]? RewriteUrlHosts(
        ReadOnlySpan<byte> packet,
        IReadOnlyList<DiscoveryAddressMapEntry> maps,
        Action<IPAddress, IPAddress>? addressRewritten)
    {
        string text;
        try
        {
            text = StrictUtf8.GetString(packet);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }

        StringBuilder? builder = null;
        int copyFrom = 0;
        int searchFrom = 0;

        while (searchFrom < text.Length)
        {
            int schemeSeparator = text.IndexOf("://", searchFrom, StringComparison.Ordinal);
            if (schemeSeparator < 0)
            {
                break;
            }

            int schemeStart = FindSchemeStart(text, schemeSeparator);
            if (schemeStart < 0)
            {
                searchFrom = schemeSeparator + 3;
                continue;
            }

            int authorityStart = schemeSeparator + 3;
            int authorityEnd = FindAuthorityEnd(text, authorityStart);
            if (authorityEnd <= authorityStart)
            {
                searchFrom = authorityStart;
                continue;
            }

            int hostStart = FindHostStart(text, authorityStart, authorityEnd);
            if (hostStart >= authorityEnd || text[hostStart] == '[')
            {
                searchFrom = authorityEnd;
                continue;
            }

            int hostEnd = FindHostEnd(text, hostStart, authorityEnd);
            if (TryParseDottedDecimal(text, hostStart, hostEnd - hostStart, out byte a, out byte b, out byte c, out byte d) &&
                TryMapAddress(a, b, c, d, maps, out string mappedAddress, out IPAddress originalIp, out IPAddress mappedIp))
            {
                builder ??= new StringBuilder(text.Length + 16);
                builder.Append(text, copyFrom, hostStart - copyFrom);
                builder.Append(mappedAddress);
                addressRewritten?.Invoke(originalIp, mappedIp);
                copyFrom = hostEnd;
            }

            searchFrom = authorityEnd;
        }

        if (builder is null)
        {
            return null;
        }

        builder.Append(text, copyFrom, text.Length - copyFrom);
        return StrictUtf8.GetBytes(builder.ToString());
    }

    private static byte[]? RewriteDnsLikeAddresses(
        DiscoveryProtocolInfo protocol,
        IDiscoveryProtocolMatcher matcher,
        ReadOnlyMemory<byte> packet,
        IReadOnlyList<DiscoveryAddressMapEntry> maps,
        Action<IPAddress, IPAddress>? addressRewritten)
    {
        ReadOnlySpan<byte> span = packet.Span;
        if (span.Length < 12)
        {
            return null;
        }

        int questionCount = DiscoveryProtocolDnsPacket.ReadUInt16(span, 4);
        int answerCount = DiscoveryProtocolDnsPacket.ReadUInt16(span, 6);
        int authorityCount = DiscoveryProtocolDnsPacket.ReadUInt16(span, 8);
        int additionalCount = DiscoveryProtocolDnsPacket.ReadUInt16(span, 10);
        int offset = 12;

        for (int i = 0; i < questionCount; i++)
        {
            if (!DiscoveryProtocolDnsPacket.TryReadQuestion(span, ref offset, out _, out _, out _))
            {
                return null;
            }
        }

        bool rewriteNbRecords = protocol.Port == 137 || matcher is DiscoveryProtocolMatcherNbns;
        byte[]? rewritten = null;
        int recordCount = answerCount + authorityCount + additionalCount;

        for (int i = 0; i < recordCount; i++)
        {
            if (!DiscoveryProtocolDnsPacket.TryReadResourceRecordHeader(
                    span,
                    ref offset,
                    out _,
                    out ushort type,
                    out _,
                    out ushort dataLength))
            {
                break;
            }

            if (offset + dataLength > span.Length)
            {
                break;
            }

            if (type == 1 && dataLength == 4)
            {
                TryRewriteDnsAddress(packet, ref rewritten, offset, maps, addressRewritten);
            }
            else if (rewriteNbRecords && type == 32 && dataLength >= 6)
            {
                for (int dataOffset = offset + 2; dataOffset + 4 <= offset + dataLength; dataOffset += 6)
                {
                    TryRewriteDnsAddress(packet, ref rewritten, dataOffset, maps, addressRewritten);
                }
            }

            offset += dataLength;
        }

        return rewritten;
    }

    private static void TryRewriteDnsAddress(
        ReadOnlyMemory<byte> packet,
        ref byte[]? rewritten,
        int offset,
        IReadOnlyList<DiscoveryAddressMapEntry> maps,
        Action<IPAddress, IPAddress>? addressRewritten)
    {
        ReadOnlySpan<byte> address = packet.Span.Slice(offset, 4);
        Span<byte> mapped = stackalloc byte[4];
        if (!TryMapAddress(address, mapped, maps) || address.SequenceEqual(mapped))
        {
            return;
        }

        rewritten ??= packet.ToArray();
        mapped.CopyTo(rewritten.AsSpan(offset, 4));
        addressRewritten?.Invoke(new IPAddress(address), new IPAddress(mapped));
    }

    private static bool TryMapAddress(
        byte a,
        byte b,
        byte c,
        byte d,
        IReadOnlyList<DiscoveryAddressMapEntry> maps,
        out string mappedAddress,
        out IPAddress originalIp,
        out IPAddress mappedIp)
    {
        Span<byte> address = stackalloc byte[4];
        address[0] = a;
        address[1] = b;
        address[2] = c;
        address[3] = d;
        Span<byte> mapped = stackalloc byte[4];
        if (!TryMapAddress(address, mapped, maps) || address.SequenceEqual(mapped))
        {
            mappedAddress = string.Empty;
            originalIp = IPAddress.None;
            mappedIp = IPAddress.None;
            return false;
        }

        mappedAddress = $"{mapped[0]}.{mapped[1]}.{mapped[2]}.{mapped[3]}";
        originalIp = new IPAddress(address);
        mappedIp = new IPAddress(mapped);
        return true;
    }

    private static bool TryMapAddress(
        ReadOnlySpan<byte> address,
        Span<byte> mapped,
        IReadOnlyList<DiscoveryAddressMapEntry> maps)
    {
        foreach (DiscoveryAddressMapEntry map in maps)
        {
            if (map.TryMapRealToMapped(address, mapped))
            {
                return true;
            }
        }

        return false;
    }

    private static void RaiseXmlElementRewrite(
        string localName,
        ReadOnlySpan<byte> originalPayload,
        ReadOnlySpan<byte> rewrittenPayload,
        Action<string, string, string>? payloadRewritten)
    {
        if (payloadRewritten is null ||
            !DiscoveryProtocolXmlLite.TryGetElementText(originalPayload, localName, out ReadOnlySpan<byte> originalValue) ||
            !DiscoveryProtocolXmlLite.TryGetElementText(rewrittenPayload, localName, out ReadOnlySpan<byte> rewrittenValue))
        {
            return;
        }

        payloadRewritten(localName, StrictUtf8.GetString(originalValue), StrictUtf8.GetString(rewrittenValue));
    }

    private static int FindSchemeStart(string text, int schemeSeparator)
    {
        int start = schemeSeparator - 1;
        while (start >= 0 && IsSchemeChar(text[start]))
        {
            start--;
        }

        start++;
        return start < schemeSeparator && char.IsAsciiLetter(text[start]) ? start : -1;
    }

    private static bool IsSchemeChar(char value)
    {
        return char.IsAsciiLetterOrDigit(value) || value is '+' or '-' or '.';
    }

    private static int FindAuthorityEnd(string text, int start)
    {
        int offset = start;
        while (offset < text.Length)
        {
            char value = text[offset];
            if (value is '/' or '?' or '#' or '<' or '>' or '"' or '\'' ||
                char.IsWhiteSpace(value))
            {
                break;
            }

            offset++;
        }

        return offset;
    }

    private static int FindHostStart(string text, int authorityStart, int authorityEnd)
    {
        int at = text.LastIndexOf('@', authorityEnd - 1, authorityEnd - authorityStart);
        return at >= authorityStart ? at + 1 : authorityStart;
    }

    private static int FindHostEnd(string text, int hostStart, int authorityEnd)
    {
        int offset = hostStart;
        while (offset < authorityEnd)
        {
            char value = text[offset];
            if (value == ':')
            {
                break;
            }

            offset++;
        }

        return offset;
    }

    private static bool TryParseDottedDecimal(
        string text,
        int start,
        int length,
        out byte a,
        out byte b,
        out byte c,
        out byte d)
    {
        Span<byte> parts = stackalloc byte[4];
        int part = 0;
        int value = 0;
        int digits = 0;

        for (int i = 0; i < length; i++)
        {
            char current = text[start + i];
            if (current == '.')
            {
                if (digits == 0 || part >= 3)
                {
                    a = b = c = d = 0;
                    return false;
                }

                parts[part++] = (byte)value;
                value = 0;
                digits = 0;
                continue;
            }

            if (!char.IsAsciiDigit(current))
            {
                a = b = c = d = 0;
                return false;
            }

            value = (value * 10) + current - '0';
            if (value > 255)
            {
                a = b = c = d = 0;
                return false;
            }

            digits++;
        }

        if (digits == 0 || part != 3)
        {
            a = b = c = d = 0;
            return false;
        }

        parts[part] = (byte)value;
        a = parts[0];
        b = parts[1];
        c = parts[2];
        d = parts[3];
        return true;
    }
}
