using Microsoft;
using System.Buffers.Binary;
using System.Net;

namespace linker.messenger.tunnel.stun.messages;

/// <summary>
/// https://tools.ietf.org/html/rfc5389#section-15.2
/// </summary>
public class XorMappedAddressStunAttributeValue : AddressStunAttributeValue
{
	private readonly byte[] _magicCookieAndTransactionId;

	public XorMappedAddressStunAttributeValue(ReadOnlySpan<byte> magicCookieAndTransactionId)
	{
		_magicCookieAndTransactionId = magicCookieAndTransactionId.ToArray();
	}

	public override int WriteTo(Span<byte> buffer)
	{
		buffer[0] = 0;
		buffer[1] = (byte)Family;
		BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], Xor(Port));
		Xor(Address).TryWriteBytes(buffer[4..], out int bytesWritten);

        return 4 + bytesWritten;
	}

	public override bool TryParse(ReadOnlySpan<byte> buffer)
	{
		if (!base.TryParse(buffer))
		{
			return false;
		}

		Port = Xor(Port);

		Address = Xor(Address);

		return true;
	}

	private ushort Xor(ushort port)
	{
		Span<byte> span = stackalloc byte[2];
		BinaryPrimitives.WriteUInt16BigEndian(span, port);
		span[0] ^= _magicCookieAndTransactionId[0];
		span[1] ^= _magicCookieAndTransactionId[1];
		return BinaryPrimitives.ReadUInt16BigEndian(span);
	}

	private IPAddress Xor(IPAddress address)
	{
		Span<byte> b = stackalloc byte[16];
		address.TryWriteBytes(b, out int bytesWritten);


        for (int i = 0; i < bytesWritten; ++i)
		{
			b[i] ^= _magicCookieAndTransactionId[i];
		}

		return new IPAddress(b[..bytesWritten]);
	}
}
