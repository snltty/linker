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

        public ValueTask<ReadOnlyMemory<byte>> ReadPacketsAsync(CancellationToken token = default)
        {
            return ReadPacketsAsync(packetDst.Memory, maxDecodePacketCount, token);
        }
        public async ValueTask<ReadOnlyMemory<byte>> ReadPacketsAsync(Memory<byte> decodeBuffer, int maxDecodePacketCount, CancellationToken token = default)
        {
            ReadResult result = await pipe.Reader.ReadAsync(token).ConfigureAwait(false);
            if (result.IsCompleted && result.Buffer.IsEmpty)
            {
                IsCompleted = true;
                return Memory<byte>.Empty;
            }
            if (result.Buffer.Length < 2)
            {
                pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                return Memory<byte>.Empty;
            }

            ReadOnlySequence<byte> buffer = result.Buffer;
            int offset = 0, packetCount = 0;
            SequencePosition consumed = result.Buffer.Start, examined = result.Buffer.End;
            do
            {
                buffer.Slice(0, 2).CopyTo(decodeBuffer.Span);
                int packetLength = decodeBuffer.Span.ToUInt16();
                if (packetLength + 2 > buffer.Length)
                {
                    break;
                }
                offset += 2 + packetLength;
                packetCount++;

                if (offset > decodeBuffer.Length || packetCount > maxDecodePacketCount)
                {
                    offset -= 2 + packetLength;
                    examined = result.Buffer.GetPosition(offset);
                    break;
                }

                buffer = buffer.Slice(2 + packetLength);
                consumed = result.Buffer.GetPosition(offset);

            } while (buffer.Length > 2);

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
