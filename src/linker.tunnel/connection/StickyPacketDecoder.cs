using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace linker.tunnel.connection
{
    public sealed class StickyPacketDecoder : IDisposable
    {
        private readonly long maxRemaining = 0;
        private int recvRemaining = 0;
        public int RecvBufferRemaining => recvRemaining;
        public long RecvBufferFree => maxRemaining - recvRemaining;
        public long ReceiveBytes { get; private set; }
        public bool IsCompleted { get; private set; }

        private readonly Pipe pipe;
        private IMemoryOwner<byte> packetBuffer;
        public StickyPacketDecoder(long maxRemaining)
        {
            this.maxRemaining = maxRemaining;

            pipe = new Pipe(new PipeOptions(pauseWriterThreshold: maxRemaining, resumeWriterThreshold: (maxRemaining / 2), useSynchronizationContext: false, minimumSegmentSize: 8192));
            this.packetBuffer = MemoryPool<byte>.Shared.Rent(4 * 1024);
        }

        public Memory<byte> GetMemory(int sizeHint = 8 * 1024)
        {
            return pipe.Writer.GetMemory(sizeHint);
        }
        public ValueTask<FlushResult> FlushAsync(int length, CancellationToken token)
        {
            Interlocked.Add(ref recvRemaining, length);
            pipe.Writer.Advance(length);
            return pipe.Writer.FlushAsync(token);
        }

        /// <summary>
        /// 获取完整包数据，多个包连在一起，返回的数据包含包长度标识（4字节）和包内容
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async ValueTask<Memory<byte>> ReadAsync(CancellationToken token)
        {
            ReadResult result = await pipe.Reader.ReadAsync(token).ConfigureAwait(false);
            if (result.IsCompleted && result.Buffer.IsEmpty)
            {
                IsCompleted = true;
                return Memory<byte>.Empty;
            }

            ReadOnlySequence<byte> buffer = result.Buffer;
            if (packetBuffer.Memory.Length < buffer.Length)
            {
                packetBuffer.Dispose();
                packetBuffer = MemoryPool<byte>.Shared.Rent((int)buffer.Length);
            }

            long offset = 0;

            do
            {
                Memory<byte> packet = packetBuffer.Memory.Slice((int)offset);

                //读取包长度
                int packetLength = 0;
                if (buffer.First.Length >= 4)
                {
                    packetLength = Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(buffer.First.Span));
                }
                else
                {
                    //长度标识跨段了
                    buffer.Slice(0, 4).CopyTo(packet.Span);
                    packetLength = Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(packet.Span));
                }
                //数据量不够
                if (packetLength + 4 > buffer.Length)
                {
                    break;
                }

                //复制一份
                buffer.Slice(0, packetLength + 4).CopyTo(packet.Span);
                ReceiveBytes += packetLength + 4;
                Interlocked.Add(ref recvRemaining, -(packetLength + 4));

                //移动位置
                offset += 4 + packetLength;
                //去掉已处理部分
                buffer = buffer.Slice(4 + packetLength);

            } while (buffer.Length > 4);

            //告诉管道已经处理了多少数据，检查了多少数据
            pipe.Reader.AdvanceTo(result.Buffer.GetPosition(offset), result.Buffer.End);

            return packetBuffer.Memory.Slice(0, (int)offset);
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
            packetBuffer?.Dispose();

            GC.Collect();
        }
    }

}
