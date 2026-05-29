using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace linker.kcp;

internal sealed class SpscReceiveBuffer : IDisposable
{
    private const int LengthPrefixSize = sizeof(ushort);
    private const int MinimumCapacity = 64 * 1024;

    private readonly object _syncRoot = new();
    private byte[] _buffer;
    private int _readIndex;
    private int _writeIndex;
    private bool _completed;
    private bool _disposed;
    private TaskCompletionSource? _dataAvailable;

    public SpscReceiveBuffer(int initialCapacity)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, MinimumCapacity));
    }

    public int WriteRecordFromKcp(Kcp kcp, int payloadLength)
    {
        if (payloadLength < 0)
        {
            return -1;
        }

        if (payloadLength > ushort.MaxValue)
        {
            throw new InvalidDataException("KCP payload length exceeds the 2-byte receive record prefix limit.");
        }

        var frameLength = checked(LengthPrefixSize + payloadLength);
        TaskCompletionSource? waiter = null;

        try
        {
            lock (_syncRoot)
            {
                if (_completed || _disposed)
                {
                    return 0;
                }

                EnsureWritableLocked(frameLength);
                var payload = _buffer.AsSpan(_writeIndex + LengthPrefixSize, payloadLength);
                var received = kcp.Recv(payload);
                if (received <= 0)
                {
                    return received;
                }

                if (received > ushort.MaxValue)
                {
                    throw new InvalidDataException("KCP payload length exceeds the 2-byte receive record prefix limit.");
                }

                BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_writeIndex, LengthPrefixSize), (ushort)received);
                _writeIndex += LengthPrefixSize + received;
                waiter = _dataAvailable;
                _dataAvailable = null;
                return received;
            }
        }
        finally
        {
            waiter?.TrySetResult();
        }
    }

    public async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task waitTask;
            lock (_syncRoot)
            {
                ThrowIfDisposedLocked();

                if (_writeIndex > _readIndex)
                {
                    var written = CopyCompleteFramesLocked(destination.Span);
                    if (written > 0)
                    {
                        return written;
                    }

                    if (TryGetFirstFrameLengthLocked(out var firstFrameLength) && firstFrameLength > destination.Length)
                    {
                        throw new ArgumentException("The receive buffer is smaller than the next framed KCP payload.", nameof(destination));
                    }
                }

                if (_completed)
                {
                    return 0;
                }

                _dataAvailable ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                waitTask = _dataAvailable.Task;
            }

            await waitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask<int> ReadRecordsAsync(KcpReceiveRecordHandler handler, CancellationToken cancellationToken)
    {
        const int maxRecordsPerRead = 64;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task waitTask;
            lock (_syncRoot)
            {
                ThrowIfDisposedLocked();

                if (_writeIndex > _readIndex)
                {
                    var count = ConsumeCompleteFramesLocked(handler, maxRecordsPerRead);
                    if (count > 0)
                    {
                        return count;
                    }
                }

                if (_completed)
                {
                    return 0;
                }

                _dataAvailable ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                waitTask = _dataAvailable.Task;
            }

            await waitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void Complete()
    {
        TaskCompletionSource? waiter;
        lock (_syncRoot)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            waiter = _dataAvailable;
            _dataAvailable = null;
        }

        waiter?.TrySetResult();
    }

    public void Dispose()
    {
        TaskCompletionSource? waiter;
        byte[]? buffer;
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _completed = true;
            waiter = _dataAvailable;
            _dataAvailable = null;
            buffer = _buffer;
            _buffer = [];
            _readIndex = 0;
            _writeIndex = 0;
        }

        ArrayPool<byte>.Shared.Return(buffer);
        waiter?.TrySetResult();
    }

    private int CopyCompleteFramesLocked(Span<byte> destination)
    {
        var scan = _readIndex;
        var written = 0;

        while (_writeIndex - scan >= LengthPrefixSize)
        {
            var frameLength = ReadFrameLengthLocked(scan);
            if (_writeIndex - scan < frameLength || destination.Length - written < frameLength)
            {
                break;
            }

            scan += frameLength;
            written += frameLength;
        }

        if (written <= 0)
        {
            return 0;
        }

        _buffer.AsSpan(_readIndex, written).CopyTo(destination);
        _readIndex += written;
        ResetIndicesIfEmptyLocked();
        return written;
    }

    private bool TryGetFirstFrameLengthLocked(out int frameLength)
    {
        frameLength = 0;
        if (_writeIndex - _readIndex < LengthPrefixSize)
        {
            return false;
        }

        frameLength = ReadFrameLengthLocked(_readIndex);
        return true;
    }

    private int ConsumeCompleteFramesLocked(KcpReceiveRecordHandler handler, int maxRecords)
    {
        var count = 0;
        var scan = _readIndex;

        while (count < maxRecords && _writeIndex - scan >= LengthPrefixSize)
        {
            var frameLength = ReadFrameLengthLocked(scan);
            if (_writeIndex - scan < frameLength)
            {
                break;
            }

            var payloadOffset = scan + LengthPrefixSize;
            var payloadLength = frameLength - LengthPrefixSize;
            handler(_buffer.AsSpan(payloadOffset, payloadLength));

            scan += frameLength;
            count++;
        }

        if (count <= 0)
        {
            return 0;
        }

        _readIndex = scan;
        ResetIndicesIfEmptyLocked();
        return count;
    }

    private int ReadFrameLengthLocked(int index)
    {
        var payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(index, LengthPrefixSize));
        return LengthPrefixSize + payloadLength;
    }

    private void EnsureWritableLocked(int length)
    {
        if (_buffer.Length - _writeIndex >= length)
        {
            return;
        }

        CompactLocked();
        if (_buffer.Length - _writeIndex >= length)
        {
            return;
        }

        GrowLocked(length);
    }

    private void CompactLocked()
    {
        if (_readIndex == 0)
        {
            return;
        }

        var readable = _writeIndex - _readIndex;
        if (readable > 0)
        {
            Buffer.BlockCopy(_buffer, _readIndex, _buffer, 0, readable);
        }

        _readIndex = 0;
        _writeIndex = readable;
    }

    private void GrowLocked(int requiredWritable)
    {
        var readable = _writeIndex - _readIndex;
        var required = readable + requiredWritable;
        var newSize = _buffer.Length;
        while (newSize < required)
        {
            newSize = newSize < 1024 * 1024
                ? newSize * 2
                : newSize + (newSize / 2);
        }

        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        if (readable > 0)
        {
            Buffer.BlockCopy(_buffer, _readIndex, newBuffer, 0, readable);
        }

        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
        _readIndex = 0;
        _writeIndex = readable;
    }

    private void ResetIndicesIfEmptyLocked()
    {
        if (_readIndex == _writeIndex)
        {
            _readIndex = 0;
            _writeIndex = 0;
        }
    }

    private void ThrowIfDisposedLocked()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
