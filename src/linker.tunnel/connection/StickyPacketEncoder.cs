using System.IO.Pipelines;

namespace linker.tunnel.connection
{
    public sealed class StickyPacketEncoder : IDisposable
    {
        private readonly long maxRemaining = 0;
        private long sendRemaining = 0;
        public long SendBufferRemaining { get => sendRemaining; }
        public long SendBufferFree { get => maxRemaining - sendRemaining; }

        public long SendBytes { get; private set; }
        public bool IsCompleted { get; private set; }

        private readonly Pipe pipe;
        public StickyPacketEncoder(long maxRemaining)
        {
            this.maxRemaining = maxRemaining;

            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxRemaining, resumeWriterThreshold: (maxRemaining / 2), useSynchronizationContext: false, minimumSegmentSize: 8192));
        }

        public ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token)
        {
            Interlocked.Add(ref sendRemaining, data.Length);
            return pipe.Writer.WriteAsync(data, token);
        }

        public ValueTask<ReadResult> ReadAsync(CancellationToken token)
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

        public void Dispose()
        {
            try
            {
                pipe?.Writer.Complete();
                pipe?.Reader.Complete();
            }
            catch (Exception)
            { }

            GC.Collect();
        }
    }
}
