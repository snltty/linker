using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace linker.stun;

public static class StunMessageCodec
{
    private const byte AddressFamilyIpv4 = 0x01;
    private const byte AddressFamilyIpv6 = 0x02;

    public static int WriteBindingRequest(
        Span<byte> destination,
        ReadOnlySpan<byte> transactionId,
        StunChangeRequest changeRequest = StunChangeRequest.None,
        string? software = "linker.stun")
    {
        ValidateDestination(destination);
        ValidateTransactionId(transactionId);

        WriteHeader(destination, StunConstants.BindingRequest, transactionId);
        var offset = StunConstants.HeaderSize;

        if (changeRequest != StunChangeRequest.None)
        {
            Span<byte> value = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(value, (uint)changeRequest);
            offset = WriteAttribute(destination, offset, StunConstants.AttributeChangeRequest, value);
        }

        if (!string.IsNullOrEmpty(software))
        {
            var softwareByteCount = Encoding.UTF8.GetByteCount(software);
            if (softwareByteCount > 763)
            {
                throw new ArgumentOutOfRangeException(nameof(software), "SOFTWARE attribute is too long.");
            }

            Span<byte> softwareBuffer = softwareByteCount <= 256
                ? stackalloc byte[softwareByteCount]
                : new byte[softwareByteCount];
            Encoding.UTF8.GetBytes(software, softwareBuffer);
            offset = WriteAttribute(destination, offset, StunConstants.AttributeSoftware, softwareBuffer);
        }

        WriteMessageLength(destination, offset);
        return offset;
    }

    public static int WriteBindingSuccessResponse(
        Span<byte> destination,
        ReadOnlySpan<byte> transactionId,
        IPEndPoint xorMappedAddress,
        IPEndPoint? mappedAddress = null,
        IPEndPoint? responseOrigin = null,
        IPEndPoint? otherAddress = null,
        string? software = "linker.stun")
    {
        ValidateDestination(destination);
        ValidateTransactionId(transactionId);

        WriteHeader(destination, StunConstants.BindingSuccessResponse, transactionId);
        var offset = StunConstants.HeaderSize;

        offset = WriteAddressAttribute(destination, offset, StunConstants.AttributeXorMappedAddress, xorMappedAddress, transactionId, xor: true);

        if (mappedAddress is not null)
        {
            offset = WriteAddressAttribute(destination, offset, StunConstants.AttributeMappedAddress, mappedAddress, transactionId, xor: false);
        }

        if (responseOrigin is not null)
        {
            offset = WriteAddressAttribute(destination, offset, StunConstants.AttributeResponseOrigin, responseOrigin, transactionId, xor: false);
        }

        if (otherAddress is not null)
        {
            offset = WriteAddressAttribute(destination, offset, StunConstants.AttributeOtherAddress, otherAddress, transactionId, xor: false);
        }

        if (!string.IsNullOrEmpty(software))
        {
            var softwareByteCount = Encoding.UTF8.GetByteCount(software);
            Span<byte> softwareBuffer = softwareByteCount <= 256
                ? stackalloc byte[softwareByteCount]
                : new byte[softwareByteCount];
            Encoding.UTF8.GetBytes(software, softwareBuffer);
            offset = WriteAttribute(destination, offset, StunConstants.AttributeSoftware, softwareBuffer);
        }

        WriteMessageLength(destination, offset);
        return offset;
    }

    public static int WriteBindingErrorResponse(
        Span<byte> destination,
        ReadOnlySpan<byte> transactionId,
        int errorCode,
        string reason)
    {
        ValidateDestination(destination);
        ValidateTransactionId(transactionId);

        if (errorCode < 300 || errorCode > 699)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode), "STUN error code must be between 300 and 699.");
        }

        WriteHeader(destination, StunConstants.BindingErrorResponse, transactionId);
        var reasonByteCount = Encoding.UTF8.GetByteCount(reason);
        Span<byte> value = reasonByteCount + 4 <= 256
            ? stackalloc byte[reasonByteCount + 4]
            : new byte[reasonByteCount + 4];

        value[2] = (byte)(errorCode / 100);
        value[3] = (byte)(errorCode % 100);
        Encoding.UTF8.GetBytes(reason, value[4..]);

        var offset = WriteAttribute(destination, StunConstants.HeaderSize, StunConstants.AttributeErrorCode, value);
        WriteMessageLength(destination, offset);
        return offset;
    }

    public static bool TryParse(ReadOnlySpan<byte> packet, out StunMessage? message, out string? error)
    {
        message = null;
        error = null;

        if (packet.Length < StunConstants.HeaderSize)
        {
            error = "STUN packet is shorter than the fixed header.";
            return false;
        }

        if ((packet[0] & 0xC0) != 0)
        {
            error = "The two most significant STUN message bits must be zero.";
            return false;
        }

        var messageType = BinaryPrimitives.ReadUInt16BigEndian(packet);
        var messageLength = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
        if ((messageLength & 0x03) != 0)
        {
            error = "STUN message length must be a multiple of four.";
            return false;
        }

        var totalLength = StunConstants.HeaderSize + messageLength;
        if (packet.Length < totalLength)
        {
            error = "STUN packet is shorter than the header length field.";
            return false;
        }

        var magicCookie = BinaryPrimitives.ReadUInt32BigEndian(packet[4..]);
        if (magicCookie != StunConstants.MagicCookie)
        {
            error = "Unsupported STUN magic cookie.";
            return false;
        }

        var transactionId = packet.Slice(8, StunConstants.TransactionIdLength).ToArray();
        var attributes = new List<StunAttribute>();
        IPEndPoint? xorMappedAddress = null;
        IPEndPoint? mappedAddress = null;
        IPEndPoint? otherAddress = null;
        IPEndPoint? responseOrigin = null;
        IPEndPoint? alternateServer = null;
        StunError? stunError = null;

        var offset = StunConstants.HeaderSize;
        while (offset < totalLength)
        {
            if (totalLength - offset < 4)
            {
                error = "STUN attribute header is truncated.";
                return false;
            }

            var type = BinaryPrimitives.ReadUInt16BigEndian(packet[offset..]);
            var length = BinaryPrimitives.ReadUInt16BigEndian(packet[(offset + 2)..]);
            offset += 4;

            if (offset + length > totalLength)
            {
                error = "STUN attribute value is truncated.";
                return false;
            }

            var value = packet.Slice(offset, length);
            var rawValue = value.ToArray();
            attributes.Add(new StunAttribute(type, rawValue));

            switch (type)
            {
                case StunConstants.AttributeXorMappedAddress:
                {
                    if (TryParseAddress(value, transactionId, xor: true, out var endpoint))
                    {
                        xorMappedAddress = endpoint;
                    }
                    break;
                }

                case StunConstants.AttributeMappedAddress:
                {
                    if (TryParseAddress(value, transactionId, xor: false, out var endpoint))
                    {
                        mappedAddress = endpoint;
                    }
                    break;
                }

                case StunConstants.AttributeOtherAddress:
                {
                    if (TryParseAddress(value, transactionId, xor: false, out var endpoint))
                    {
                        otherAddress = endpoint;
                    }
                    break;
                }

                case StunConstants.AttributeResponseOrigin:
                {
                    if (TryParseAddress(value, transactionId, xor: false, out var endpoint))
                    {
                        responseOrigin = endpoint;
                    }
                    break;
                }

                case StunConstants.AttributeAlternateServer:
                {
                    if (TryParseAddress(value, transactionId, xor: false, out var endpoint))
                    {
                        alternateServer = endpoint;
                    }
                    break;
                }

                case StunConstants.AttributeErrorCode:
                    stunError = ParseError(value);
                    break;
            }

            offset += length + Pad4(length);
        }

        message = new StunMessage(
            messageType,
            transactionId,
            attributes,
            xorMappedAddress,
            mappedAddress,
            otherAddress,
            responseOrigin,
            alternateServer,
            stunError);
        return true;
    }

    private static void ValidateDestination(Span<byte> destination)
    {
        if (destination.Length < StunConstants.HeaderSize)
        {
            throw new ArgumentException("Destination is too small for a STUN header.", nameof(destination));
        }
    }

    private static void ValidateTransactionId(ReadOnlySpan<byte> transactionId)
    {
        if (transactionId.Length != StunConstants.TransactionIdLength)
        {
            throw new ArgumentException("STUN transaction id must be 12 bytes.", nameof(transactionId));
        }
    }

    private static void WriteHeader(Span<byte> destination, ushort messageType, ReadOnlySpan<byte> transactionId)
    {
        destination.Clear();
        BinaryPrimitives.WriteUInt16BigEndian(destination, messageType);
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], 0);
        BinaryPrimitives.WriteUInt32BigEndian(destination[4..], StunConstants.MagicCookie);
        transactionId.CopyTo(destination[8..]);
    }

    private static void WriteMessageLength(Span<byte> destination, int offset)
    {
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], checked((ushort)(offset - StunConstants.HeaderSize)));
    }

    private static int WriteAttribute(Span<byte> destination, int offset, ushort type, ReadOnlySpan<byte> value)
    {
        var paddedLength = Pad4(value.Length);
        if (destination.Length - offset < 4 + value.Length + paddedLength)
        {
            throw new ArgumentException("Destination is too small for the STUN attribute.", nameof(destination));
        }

        BinaryPrimitives.WriteUInt16BigEndian(destination[offset..], type);
        BinaryPrimitives.WriteUInt16BigEndian(destination[(offset + 2)..], checked((ushort)value.Length));
        offset += 4;
        value.CopyTo(destination[offset..]);
        offset += value.Length;

        destination.Slice(offset, paddedLength).Clear();
        return offset + paddedLength;
    }

    private static int WriteAddressAttribute(
        Span<byte> destination,
        int offset,
        ushort type,
        IPEndPoint endpoint,
        ReadOnlySpan<byte> transactionId,
        bool xor)
    {
        Span<byte> value = stackalloc byte[20];
        value.Clear();

        var addressBytes = endpoint.Address.GetAddressBytes();
        if (addressBytes.Length == 4)
        {
            value[1] = AddressFamilyIpv4;
            var port = xor
                ? (ushort)(endpoint.Port ^ (int)(StunConstants.MagicCookie >> 16))
                : (ushort)endpoint.Port;
            BinaryPrimitives.WriteUInt16BigEndian(value[2..], port);

            var address = BinaryPrimitives.ReadUInt32BigEndian(addressBytes);
            if (xor)
            {
                address ^= StunConstants.MagicCookie;
            }

            BinaryPrimitives.WriteUInt32BigEndian(value[4..], address);
            return WriteAttribute(destination, offset, type, value[..8]);
        }

        if (addressBytes.Length == 16)
        {
            value[1] = AddressFamilyIpv6;
            var port = xor
                ? (ushort)(endpoint.Port ^ (int)(StunConstants.MagicCookie >> 16))
                : (ushort)endpoint.Port;
            BinaryPrimitives.WriteUInt16BigEndian(value[2..], port);
            addressBytes.CopyTo(value[4..]);

            if (xor)
            {
                XorIpv6Address(value[4..20], transactionId);
            }

            return WriteAttribute(destination, offset, type, value);
        }

        throw new NotSupportedException("Only IPv4 and IPv6 addresses are supported.");
    }

    private static bool TryParseAddress(ReadOnlySpan<byte> value, ReadOnlySpan<byte> transactionId, bool xor, out IPEndPoint? endpoint)
    {
        endpoint = null;
        if (value.Length < 4 || value[0] != 0)
        {
            return false;
        }

        var family = value[1];
        var port = BinaryPrimitives.ReadUInt16BigEndian(value[2..]);
        if (xor)
        {
            port = (ushort)(port ^ (StunConstants.MagicCookie >> 16));
        }

        if (family == AddressFamilyIpv4)
        {
            if (value.Length < 8)
            {
                return false;
            }

            var address = BinaryPrimitives.ReadUInt32BigEndian(value[4..]);
            if (xor)
            {
                address ^= StunConstants.MagicCookie;
            }

            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, address);
            endpoint = new IPEndPoint(new IPAddress(bytes), port);
            return true;
        }

        if (family == AddressFamilyIpv6)
        {
            if (value.Length < 20 || transactionId.Length != StunConstants.TransactionIdLength)
            {
                return false;
            }

            Span<byte> bytes = stackalloc byte[16];
            value.Slice(4, 16).CopyTo(bytes);
            if (xor)
            {
                XorIpv6Address(bytes, transactionId);
            }

            endpoint = new IPEndPoint(new IPAddress(bytes), port);
            return true;
        }

        return false;
    }

    private static void XorIpv6Address(Span<byte> addressBytes, ReadOnlySpan<byte> transactionId)
    {
        Span<byte> mask = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(mask, StunConstants.MagicCookie);
        transactionId.CopyTo(mask[4..]);

        for (var i = 0; i < addressBytes.Length; i++)
        {
            addressBytes[i] ^= mask[i];
        }
    }

    private static StunError? ParseError(ReadOnlySpan<byte> value)
    {
        if (value.Length < 4)
        {
            return null;
        }

        var code = ((value[2] & 0x07) * 100) + value[3];
        var reason = value.Length > 4 ? Encoding.UTF8.GetString(value[4..]) : string.Empty;
        return new StunError(code, reason);
    }

    private static int Pad4(int length)
    {
        return (4 - (length & 0x03)) & 0x03;
    }
}
