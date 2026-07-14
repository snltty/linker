using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace linker.discovery;

internal static class DiscoveryProtocolPayloadRewriteHelper
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static byte[]? RewriteHttpHeaderUrlHosts(
        DiscoveryProtocolRewriteContext context,
        ReadOnlyMemory<byte> packet,
        params string[] headerNames)
    {
        if (!TryDecodeRewriteText(context, packet, out string text))
        {
            return null;
        }

        StringBuilder? builder = null;
        int copyFrom = 0;
        int offset = 0;

        while (offset < text.Length)
        {
            int lineEnd = text.IndexOf('\n', offset);
            int nextOffset;
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
                nextOffset = text.Length;
            }
            else
            {
                nextOffset = lineEnd + 1;
                if (lineEnd > offset && text[lineEnd - 1] == '\r')
                {
                    lineEnd--;
                }
            }

            int colon = text.IndexOf(':', offset, lineEnd - offset);
            if (colon > offset)
            {
                int nameStart = offset;
                int nameEnd = colon;
                while (nameStart < nameEnd && text[nameStart] <= 0x20)
                {
                    nameStart++;
                }

                while (nameEnd > nameStart && text[nameEnd - 1] <= 0x20)
                {
                    nameEnd--;
                }

                if (ContainsHeaderName(headerNames, text.AsSpan(nameStart, nameEnd - nameStart)))
                {
                    RewriteUrlHostsInRange(context, text, colon + 1, lineEnd, ref builder, ref copyFrom);
                }
            }

            offset = nextOffset;
        }

        return BuildRewriteResult(text, builder, copyFrom);
    }

    public static byte[]? RewriteXmlElementUrlHosts(
        DiscoveryProtocolRewriteContext context,
        ReadOnlyMemory<byte> packet,
        params string[] localNames)
    {
        if (!TryDecodeRewriteText(context, packet, out string text))
        {
            return null;
        }

        StringBuilder? builder = null;
        int copyFrom = 0;
        int searchFrom = 0;

        while (searchFrom < text.Length)
        {
            int open = text.IndexOf('<', searchFrom);
            if (open < 0 || open + 1 >= text.Length)
            {
                break;
            }

            char marker = text[open + 1];
            if (marker is '/' or '!' or '?')
            {
                searchFrom = open + 1;
                continue;
            }

            int nameStart = open + 1;
            int nameEnd = nameStart;
            while (nameEnd < text.Length)
            {
                char current = text[nameEnd];
                if (current is '>' or '/' || char.IsWhiteSpace(current))
                {
                    break;
                }

                nameEnd++;
            }

            if (nameEnd == nameStart)
            {
                searchFrom = open + 1;
                continue;
            }

            ReadOnlySpan<char> elementName = text.AsSpan(nameStart, nameEnd - nameStart);
            int colon = elementName.LastIndexOf(':');
            if (colon >= 0)
            {
                elementName = elementName[(colon + 1)..];
            }

            if (!ContainsLocalName(localNames, elementName))
            {
                searchFrom = nameEnd;
                continue;
            }

            int tagEnd = text.IndexOf('>', nameEnd);
            if (tagEnd < 0)
            {
                break;
            }

            int valueStart = tagEnd + 1;
            int close = text.IndexOf('<', valueStart);
            if (close < 0)
            {
                break;
            }

            RewriteUrlHostsInRange(context, text, valueStart, close, ref builder, ref copyFrom);
            searchFrom = close + 1;
        }

        return BuildRewriteResult(text, builder, copyFrom);
    }

    public static byte[]? RewriteDnsLikeAddresses(
        DiscoveryProtocolRewriteContext context,
        ReadOnlyMemory<byte> packet,
        bool rewriteNbRecords)
    {
        if (!context.HasAddressMaps)
        {
            return null;
        }

        ReadOnlySpan<byte> span = packet.Span;
        if (span.Length < 12)
        {
            return null;
        }

        if ((span[2] & 0x80) == 0)
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
                TryRewriteDnsAddress(context, packet, ref rewritten, offset);
            }
            else if (rewriteNbRecords && type == 32 && dataLength >= 6)
            {
                for (int dataOffset = offset + 2; dataOffset + 4 <= offset + dataLength; dataOffset += 6)
                {
                    TryRewriteDnsAddress(context, packet, ref rewritten, dataOffset);
                }
            }

            offset += dataLength;
        }

        return rewritten;
    }

    public static byte[]? RewriteXmlIPv4ElementTexts(
        DiscoveryProtocolRewriteContext context,
        ReadOnlyMemory<byte> packet,
        params string[] localNames)
    {
        if (!context.HasAddressMaps || packet.Length == 0)
        {
            return null;
        }

        string text;
        try
        {
            text = StrictUtf8.GetString(packet.Span);
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
            int open = text.IndexOf('<', searchFrom);
            if (open < 0 || open + 1 >= text.Length)
            {
                break;
            }

            char marker = text[open + 1];
            if (marker is '/' or '!' or '?')
            {
                searchFrom = open + 1;
                continue;
            }

            int nameStart = open + 1;
            int nameEnd = nameStart;
            while (nameEnd < text.Length)
            {
                char current = text[nameEnd];
                if (current is '>' or '/' || char.IsWhiteSpace(current))
                {
                    break;
                }

                nameEnd++;
            }

            if (nameEnd == nameStart)
            {
                searchFrom = open + 1;
                continue;
            }

            ReadOnlySpan<char> elementName = text.AsSpan(nameStart, nameEnd - nameStart);
            int colon = elementName.LastIndexOf(':');
            if (colon >= 0)
            {
                elementName = elementName[(colon + 1)..];
            }

            if (!ContainsLocalName(localNames, elementName))
            {
                searchFrom = nameEnd;
                continue;
            }

            int tagEnd = text.IndexOf('>', nameEnd);
            if (tagEnd < 0)
            {
                break;
            }

            int valueStart = tagEnd + 1;
            int close = text.IndexOf('<', valueStart);
            if (close < 0)
            {
                break;
            }

            int trimmedStart = valueStart;
            int trimmedEnd = close;
            while (trimmedStart < trimmedEnd && text[trimmedStart] <= 0x20)
            {
                trimmedStart++;
            }

            while (trimmedEnd > trimmedStart && text[trimmedEnd - 1] <= 0x20)
            {
                trimmedEnd--;
            }

            if (TryParseDottedDecimal(
                    text,
                    trimmedStart,
                    trimmedEnd - trimmedStart,
                    out byte a,
                    out byte b,
                    out byte c,
                    out byte d) &&
                TryMapAddress(context, a, b, c, d, out string mappedAddress, out IPAddress originalIp, out IPAddress mappedIp))
            {
                builder ??= new StringBuilder(text.Length + 16);
                builder.Append(text, copyFrom, trimmedStart - copyFrom);
                builder.Append(mappedAddress);
                context.ReportAddressRewrite(originalIp, mappedIp);
                context.ReportPayloadRewrite(elementName.ToString(), text[trimmedStart..trimmedEnd], mappedAddress);
                copyFrom = trimmedEnd;
            }

            searchFrom = close + 1;
        }

        if (builder is null)
        {
            return null;
        }

        builder.Append(text, copyFrom, text.Length - copyFrom);
        return StrictUtf8.GetBytes(builder.ToString());
    }

    public static void ReportXmlElementRewrite(
        DiscoveryProtocolRewriteContext context,
        string localName,
        ReadOnlySpan<byte> originalPayload,
        ReadOnlySpan<byte> rewrittenPayload)
    {
        if (!DiscoveryProtocolXmlLite.TryGetElementText(originalPayload, localName, out ReadOnlySpan<byte> originalValue) ||
            !DiscoveryProtocolXmlLite.TryGetElementText(rewrittenPayload, localName, out ReadOnlySpan<byte> rewrittenValue))
        {
            return;
        }

        try
        {
            context.ReportPayloadRewrite(localName, StrictUtf8.GetString(originalValue), StrictUtf8.GetString(rewrittenValue));
        }
        catch (DecoderFallbackException)
        {
        }
    }

    private static bool ContainsLocalName(IReadOnlyList<string> localNames, ReadOnlySpan<char> candidate)
    {
        foreach (string localName in localNames)
        {
            if (candidate.Equals(localName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsHeaderName(IReadOnlyList<string> headerNames, ReadOnlySpan<char> candidate)
    {
        foreach (string headerName in headerNames)
        {
            if (candidate.Equals(headerName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryDecodeRewriteText(
        DiscoveryProtocolRewriteContext context,
        ReadOnlyMemory<byte> packet,
        out string text)
    {
        if (!context.HasAddressMaps)
        {
            text = string.Empty;
            return false;
        }

        try
        {
            text = StrictUtf8.GetString(packet.Span);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static byte[]? BuildRewriteResult(string text, StringBuilder? builder, int copyFrom)
    {
        if (builder is null)
        {
            return null;
        }

        builder.Append(text, copyFrom, text.Length - copyFrom);
        return StrictUtf8.GetBytes(builder.ToString());
    }

    private static bool RewriteUrlHostsInRange(
        DiscoveryProtocolRewriteContext context,
        string text,
        int start,
        int end,
        ref StringBuilder? builder,
        ref int copyFrom)
    {
        bool changed = false;
        int searchFrom = start;

        while (searchFrom < end)
        {
            int schemeSeparator = text.IndexOf("://", searchFrom, end - searchFrom, StringComparison.Ordinal);
            if (schemeSeparator < 0)
            {
                break;
            }

            int schemeStart = FindSchemeStart(text, schemeSeparator);
            if (schemeStart < start)
            {
                searchFrom = schemeSeparator + 3;
                continue;
            }

            int authorityStart = schemeSeparator + 3;
            int authorityEnd = FindAuthorityEnd(text, authorityStart, end);
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
                TryMapAddress(context, a, b, c, d, out string mappedAddress, out IPAddress originalIp, out IPAddress mappedIp))
            {
                builder ??= new StringBuilder(text.Length + 16);
                builder.Append(text, copyFrom, hostStart - copyFrom);
                builder.Append(mappedAddress);
                context.ReportAddressRewrite(originalIp, mappedIp);
                copyFrom = hostEnd;
                changed = true;
            }

            searchFrom = authorityEnd;
        }

        return changed;
    }

    private static void TryRewriteDnsAddress(
        DiscoveryProtocolRewriteContext context,
        ReadOnlyMemory<byte> packet,
        ref byte[]? rewritten,
        int offset)
    {
        ReadOnlySpan<byte> address = packet.Span.Slice(offset, 4);
        Span<byte> mapped = stackalloc byte[4];
        if (!context.TryMapRealToMapped(address, mapped) || address.SequenceEqual(mapped))
        {
            return;
        }

        rewritten ??= packet.ToArray();
        mapped.CopyTo(rewritten.AsSpan(offset, 4));
        context.ReportAddressRewrite(new IPAddress(address), new IPAddress(mapped));
    }

    private static bool TryMapAddress(
        DiscoveryProtocolRewriteContext context,
        byte a,
        byte b,
        byte c,
        byte d,
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
        if (!context.TryMapRealToMapped(address, mapped) || address.SequenceEqual(mapped))
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

    private static int FindAuthorityEnd(string text, int start, int end)
    {
        int offset = start;
        while (offset < end)
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
