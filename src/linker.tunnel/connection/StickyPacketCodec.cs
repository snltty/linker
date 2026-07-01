using linker.libs.extends;
using System.Buffers;
using System.IO.Pipelines;

namespace linker.tunnel.connection
{
    public sealed class StickyPacketCodec : IDisposable
    {
        private readonly long maxRemaining = 0;
        private long sendRemaining = 0;
        public long SendBufferRemaining { get => sendRemaining; }
        public long SendBufferFree { get => maxRemaining - sendRemaining; }

        public long SendBytes { get; private set; }
        public bool IsCompleted { get; private set; }

        private readonly Pipe pipe;

        private readonly IMemoryOwner<byte> packetDst;
        private readonly int maxDecodePacketCount;

        public const int PacketLengthSize = TunnelPacket.PacketLengthSize;
        public int ReadLength(ReadOnlyMemory<byte> memory) => TunnelPacket.ReadLength(memory);

        public StickyPacketCodec(long maxRemaining, int maxDecodeBufferSize = 8 * 1024, int maxDecodePacketCount = int.MaxValue)
        {
            this.maxRemaining = maxRemaining;
            packetDst = MemoryPool<byte>.Shared.Rent(maxDecodeBufferSize);
            this.maxDecodePacketCount = maxDecodePacketCount;

            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxRemaining, resumeWriterThreshold: (maxRemaining / 2), useSynchronizationContext: false, minimumSegmentSize: 8 * 1024));
        }

        public Memory<byte> GetMemory(int sizeHint = 8 * 1024)
        {
            return pipe.Writer.GetMemory(sizeHint);
        }
        public ValueTask<FlushResult> FlushAsync(int length, CancellationToken token = default)
        {
            Interlocked.Add(ref sendRemaining, length);
            pipe.Writer.Advance(length);
            return pipe.Writer.FlushAsync(token);
        }
        public ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
        {
            Interlocked.Add(ref sendRemaining, data.Length);
            return pipe.Writer.WriteAsync(data, token);
        }
        public ValueTask<ReadResult> ReadAsync(CancellationToken token = default)
        {
            return pipe.Reader.ReadAsync(token);
        }
        public void Advance(int length)
        {
            Interlocked.Add(ref sendRemaining, -length);
            SendBytes += length;
        }
        public void AdvanceTo(SequencePosition consumed)
        {
            pipe.Reader.AdvanceTo(consumed);
        }

        public async ValueTask<ReadOnlyMemory<byte>> ReadPacketsAsync(CancellationToken token = default)
        {
            return await ReadPacketsAsync(packetDst.Memory, maxDecodePacketCount, token).ConfigureAwait(false);
        }
        public async ValueTask<ReadOnlyMemory<byte>> ReadPacketsAsync(Memory<byte> decodeBuffer, int maxDecodePacketCount, CancellationToken token = default)
        {
            ReadResult result = await pipe.Reader.ReadAsync(token).ConfigureAwait(false);
            if (result.IsCompleted && result.Buffer.IsEmpty)
            {
                IsCompleted = true;
                return Memory<byte>.Empty;
            }
            if (result.Buffer.Length < PacketLengthSize)
            {
                pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                return Memory<byte>.Empty;
            }

            ReadOnlySequence<byte> buffer = result.Buffer;
            int offset = 0, packetCount = 0;
            SequencePosition consumed = result.Buffer.Start, examined = result.Buffer.End;
            do
            {
                buffer.Slice(0, PacketLengthSize).CopyTo(decodeBuffer.Span);
                int packetLength = ReadLength(decodeBuffer);
                if (packetLength + PacketLengthSize > buffer.Length)
                {
                    break;
                }
                offset += PacketLengthSize + packetLength;
                packetCount++;

                if (offset > decodeBuffer.Length || packetCount > maxDecodePacketCount)
                {
                    offset -= PacketLengthSize + packetLength;
                    examined = result.Buffer.GetPosition(offset);
                    break;
                }

                buffer = buffer.Slice(PacketLengthSize + packetLength);
                consumed = result.Buffer.GetPosition(offset);

            } while (buffer.Length > PacketLengthSize);

            result.Buffer.Slice(0, offset).CopyTo(decodeBuffer.Span);
            Advance(offset);
            pipe.Reader.AdvanceTo(consumed, examined);
            return decodeBuffer.Slice(0, offset);
        }



        public void Dispose()
        {
            try
            {
                pipe?.Writer.Complete();
                pipe?.Reader.Complete();
            }
            catch (Exception)
            { }

            packetDst?.Dispose();

            GC.Collect();
        }
    }
}
