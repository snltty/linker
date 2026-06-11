using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;

namespace linker.kcp;

public delegate void KcpOutput(ReadOnlyMemory<byte> packet);

public enum KcpPacketType : byte
{
    Regular = 0,
    Fec = 1
}

public readonly record struct KcpDiagnostics(
    ulong InputDatagrams,
    ulong InputBytes,
    ulong InputAckSegments,
    ulong InputPushSegments,
    ulong QueuedAckSegments,
    ulong OutputDatagrams,
    ulong OutputBytes,
    ulong OutputAckSegments,
    ulong OutputPushSegments,
    ulong OutputInitialPushSegments,
    ulong OutputFastResendPushSegments,
    ulong OutputEarlyResendPushSegments,
    ulong OutputRtoResendPushSegments,
    ulong SelectiveAckedSegments,
    ulong CumulativeAckedSegments,
    ulong FastAckMarks,
    ulong FullFlushCount,
    ulong PendingFlushCount,
    ulong AckOnlyFlushCount,
    int SendQueueCount,
    int SendBufferCount,
    int ReceiveQueueCount,
    int ReceiveBufferCount,
    int AckListCount,
    uint SndUna,
    uint SndNext,
    uint RcvNext,
    uint RemoteWindow,
    uint Rto,
    int SmoothedRtt,
    int RttVariance);

public sealed class Kcp : IDisposable
{
    public const int RtoNoDelay = 30;
    public const int RtoMin = 100;
    public const int RtoDefault = 200;
    public const int RtoMax = 60_000;

    public const byte CommandPush = 81;
    public const byte CommandAck = 82;
    public const byte CommandWindowAsk = 83;
    public const byte CommandWindowSize = 84;
    public const byte CommandAckRange = 85;

    public const int DefaultSendWindow = 32;
    public const int DefaultReceiveWindow = 32;
    public const int DefaultMtu = 1400;
    public const int FastAck = 3;
    public const int DefaultInterval = 100;
    public const int Overhead = 24;
    public const int DeadLink = 20;
    public const int InitialThreshold = 2;
    public const int MinimumThreshold = 2;
    public const int ProbeInitial = 500;
    public const int ProbeLimit = 120_000;

    private const uint AskSend = 1;
    private const uint AskTell = 2;
    private const uint FlushAckOnly = 1;
    private const uint FlushFull = 2;
    private const uint FlushPendingType = 3;
    private const int AckRangePayloadSize = 12;

    private static readonly long RefTimestamp = Stopwatch.GetTimestamp();
    private static readonly double TimestampToMilliseconds = 1000d / Stopwatch.Frequency;
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;

    private readonly uint _conv;
    private readonly KcpOutput _output;
    private readonly RingBuffer<KcpSegment> _sendQueue;
    private readonly RingBuffer<KcpSegment> _receiveQueue;
    private readonly RingBuffer<KcpSegment> _sendBuffer;
    private readonly SegmentHeap _receiveBuffer;
    private readonly List<AckItem> _ackList;
    private readonly Stack<KcpSegment> _segmentPool;
    private readonly KcpSegment _flushHeader = new();

    private byte[] _buffer;
    private bool _disposed;

    private uint _mtu = DefaultMtu;
    private uint _mss = DefaultMtu - Overhead;
    private uint _state;
    private uint _sndUna;
    private uint _sndNext;
    private uint _rcvNext;
    private uint _ssthresh = InitialThreshold;
    private int _rxRttVar;
    private int _rxSrtt;
    private uint _rxRto = RtoDefault;
    private uint _rxMinRto = RtoMin;
    private uint _sendWindow = DefaultSendWindow;
    private uint _receiveWindow = DefaultReceiveWindow;
    private uint _remoteWindow = DefaultReceiveWindow;
    private uint _congestionWindow;
    private uint _increment;
    private uint _probe;
    private uint _probeTimestamp;
    private uint _probeWait;
    private uint _interval = DefaultInterval;
    private uint _flushTimestamp = DefaultInterval;
    private uint _noDelay;
    private uint _updated;
    private uint _deadLink = DeadLink;
    private int _fastResend;
    private int _noCongestionWindow;
    private int _stream;
    private ulong _inputDatagrams;
    private ulong _inputBytes;
    private ulong _inputAckSegments;
    private ulong _inputPushSegments;
    private ulong _queuedAckSegments;
    private ulong _outputDatagrams;
    private ulong _outputBytes;
    private ulong _outputAckSegments;
    private ulong _outputPushSegments;
    private ulong _outputInitialPushSegments;
    private ulong _outputFastResendPushSegments;
    private ulong _outputEarlyResendPushSegments;
    private ulong _outputRtoResendPushSegments;
    private ulong _selectiveAckedSegments;
    private ulong _cumulativeAckedSegments;
    private ulong _fastAckMarks;
    private ulong _fullFlushCount;
    private ulong _pendingFlushCount;
    private ulong _ackOnlyFlushCount;

    public Kcp(uint conv, KcpOutput output)
    {
        _conv = conv;
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _buffer = new byte[(DefaultMtu + Overhead) * 3];
        _sendBuffer = new RingBuffer<KcpSegment>(DefaultSendWindow * 2);
        _receiveQueue = new RingBuffer<KcpSegment>(DefaultReceiveWindow * 2);
        _sendQueue = new RingBuffer<KcpSegment>(DefaultSendWindow * 2);
        _receiveBuffer = new SegmentHeap(DefaultReceiveWindow * 2);
        _ackList = new List<AckItem>(DefaultReceiveWindow * 2);
        _segmentPool = new Stack<KcpSegment>(DefaultSendWindow * 4);
    }

    public uint Conv => _conv;

    public int Mtu => (int)_mtu;

    public int Mss => (int)_mss;

    public int Interval => (int)_interval;

    public uint State => _state;

    public uint Rto => _rxRto;

    public int SmoothedRtt => _rxSrtt;

    public int RttVariance => _rxRttVar;

    public bool StreamMode
    {
        get => _stream != 0;
        set => _stream = value ? 1 : 0;
    }

    public int PeekSize()
    {
        var segment = _receiveQueue.Peek();
        if (segment is null)
        {
            return -1;
        }

        if (segment.Fragment == 0)
        {
            return segment.Length;
        }

        if (_receiveQueue.Count < segment.Fragment + 1)
        {
            return -1;
        }

        var length = 0;
        var receiveQueueCount = _receiveQueue.Count;
        for (var i = 0; i < receiveQueueCount; i++)
        {
            var seg = _receiveQueue[i];
            length += seg.Length;
            if (seg.Fragment == 0)
            {
                break;
            }
        }

        return length;
    }

    public int Recv(Span<byte> destination)
    {
        ThrowIfDisposed();

        var peekSize = PeekSize();
        if (peekSize < 0)
        {
            return -1;
        }

        if (peekSize > destination.Length)
        {
            return -2;
        }

        var fastRecover = _receiveQueue.Count >= (int)_receiveWindow;
        var written = 0;
        while (_receiveQueue.Pop(out var segment))
        {
            segment!.Buffer!.AsSpan(0, segment.Length).CopyTo(destination[written..]);
            written += segment.Length;
            var fragment = segment.Fragment;
            ReleaseSegment(segment);
            if (fragment == 0)
            {
                break;
            }
        }

        MoveReceiveBufferToQueue();

        if (_receiveQueue.Count < (int)_receiveWindow && fastRecover)
        {
            _probe |= AskTell;
        }

        return written;
    }

    public int Send(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (data.Length == 0)
        {
            return -1;
        }

        if (_stream != 0)
        {
            var tail = _sendQueue.PeekTail();
            if (tail is not null && tail.Length < _mss)
            {
                var capacity = (int)_mss - tail.Length;
                var extend = Math.Min(data.Length, capacity);
                tail.EnsureCapacity(tail.Length + extend);
                data[..extend].CopyTo(tail.Buffer!.AsSpan(tail.Length));
                tail.Length += extend;
                data = data[extend..];
            }

            if (data.Length == 0)
            {
                return 0;
            }
        }

        var count = data.Length <= _mss
            ? 1
            : (data.Length + (int)_mss - 1) / (int)_mss;

        if (count > byte.MaxValue)
        {
            return -2;
        }

        for (var i = 0; i < count; i++)
        {
            var size = Math.Min(data.Length, (int)_mss);
            var segment = NewSegment(size);
            data[..size].CopyTo(segment.Buffer!.AsSpan(0, size));
            segment.Fragment = _stream == 0 ? (byte)(count - i - 1) : (byte)0;
            _sendQueue.Push(segment);
            data = data[size..];
        }

        return 0;
    }

    public int Input(ReadOnlySpan<byte> data, KcpPacketType packetType = KcpPacketType.Regular, bool ackNoDelay = false)
    {
        ThrowIfDisposed();

        var sendUna = _sndUna;
        if (data.Length < Overhead)
        {
            return -1;
        }

        _inputDatagrams++;
        _inputBytes += (ulong)data.Length;

        uint latest = 0;
        var updateRtt = false;
        var flushSegments = 0;

        while (data.Length >= Overhead)
        {
            var conv = BinaryPrimitives.ReadUInt32LittleEndian(data);
            var cmd = data[4];
            var fragment = data[5];
            var window = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
            var timestamp = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
            var sn = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
            var una = BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
            var length = BinaryPrimitives.ReadUInt32LittleEndian(data[20..]);
            data = data[Overhead..];

            if (conv != _conv)
            {
                return -1;
            }

            if (length > data.Length)
            {
                return -2;
            }

            if (cmd is not (CommandPush or CommandAck or CommandWindowAsk or CommandWindowSize or CommandAckRange))
            {
                return -3;
            }

            if (packetType == KcpPacketType.Regular)
            {
                _remoteWindow = window;
            }

            ParseUna(una);

            ShrinkBuffer();

            switch (cmd)
            {
                case CommandAck:
                    _inputAckSegments++;
                    ParseAck(sn);
                    flushSegments |= ParseFastAck(sn, timestamp);
                    updateRtt = true;
                    latest = timestamp;
                    break;
                case CommandAckRange:
                    _inputAckSegments++;
                    var ackRangeResult = ParseAckRanges(data[..(int)length], out var rangeTimestamp);
                    if (ackRangeResult < 0)
                    {
                        return ackRangeResult;
                    }

                    flushSegments |= ackRangeResult;
                    if (rangeTimestamp != 0)
                    {
                        updateRtt = true;
                        latest = rangeTimestamp;
                    }
                    break;
                case CommandPush:
                    _inputPushSegments++;
                    var repeat = true;
                    if (Timediff(sn, _rcvNext + _receiveWindow) < 0)
                    {
                        AckPush(sn, timestamp);
                        if (Timediff(sn, _rcvNext) >= 0)
                        {
                            repeat = ParseData(
                                conv,
                                cmd,
                                fragment,
                                window,
                                timestamp,
                                sn,
                                una,
                                data[..(int)length]);
                        }
                    }

                    _ = repeat;
                    break;
                case CommandWindowAsk:
                    _probe |= AskTell;
                    break;
                case CommandWindowSize:
                    break;
            }

            data = data[(int)length..];
        }

        if (updateRtt && packetType == KcpPacketType.Regular)
        {
            var current = CurrentMs();
            if (Timediff(current, latest) >= 0)
            {
                UpdateAck(Timediff(current, latest));
            }
        }

        if (_noCongestionWindow == 0 && Timediff(_sndUna, sendUna) > 0 && _congestionWindow < _remoteWindow)
        {
            var mss = _mss;
            if (_congestionWindow < _ssthresh)
            {
                _congestionWindow++;
                _increment += mss;
            }
            else
            {
                if (_increment < mss)
                {
                    _increment = mss;
                }

                _increment += (mss * mss) / _increment + (mss / 16);
                if ((_congestionWindow + 1) * mss <= _increment)
                {
                    _congestionWindow = (_increment + mss - 1) / mss;
                }
            }

            if (_congestionWindow > _remoteWindow)
            {
                _congestionWindow = _remoteWindow;
                _increment = _remoteWindow * mss;
            }
        }

        if (flushSegments != 0)
        {
            FlushCore(FlushFull);
        }
        else if (_ackList.Count >= _mtu / Overhead)
        {
            FlushCore(FlushAckOnly);
        }
        else if (ackNoDelay && _ackList.Count > 0)
        {
            FlushCore(FlushAckOnly);
        }

        return 0;
    }

    public void Update()
    {
        ThrowIfDisposed();

        var current = CurrentMs();
        if (_updated == 0)
        {
            _updated = 1;
            _flushTimestamp = current;
        }

        var slap = Timediff(current, _flushTimestamp);
        if (slap is >= 10_000 or < -10_000)
        {
            _flushTimestamp = current;
            slap = 0;
        }

        if (slap >= 0)
        {
            _flushTimestamp += _interval;
            if (Timediff(current, _flushTimestamp) >= 0)
            {
                _flushTimestamp = current + _interval;
            }

            FlushCore(FlushFull);
        }
    }

    public uint Flush()
    {
        ThrowIfDisposed();

        if (_updated == 0)
        {
            _updated = 1;
            _flushTimestamp = CurrentMs() + _interval;
        }

        return FlushCore(FlushFull);
    }

    public uint FlushPending()
    {
        ThrowIfDisposed();

        if (_updated == 0)
        {
            _updated = 1;
            _flushTimestamp = CurrentMs() + _interval;
        }

        return FlushCore(FlushPendingType);
    }

    public uint Check()
    {
        ThrowIfDisposed();

        var current = CurrentMs();
        var flushTimestamp = _flushTimestamp;
        var packetTime = int.MaxValue;

        if (_updated == 0)
        {
            return current;
        }

        if (Timediff(current, flushTimestamp) >= 10_000 || Timediff(current, flushTimestamp) < -10_000)
        {
            flushTimestamp = current;
        }

        if (Timediff(current, flushTimestamp) >= 0)
        {
            return current;
        }

        var flushTime = Timediff(flushTimestamp, current);
        var sendBufferCount = _sendBuffer.Count;
        for (var i = 0; i < sendBufferCount; i++)
        {
            var segment = _sendBuffer[i];
            var diff = Timediff(segment.ResendTimestamp, current);
            if (diff <= 0)
            {
                packetTime = 0;
                break;
            }

            if (diff < packetTime)
            {
                packetTime = diff;
            }
        }

        if (packetTime == 0)
        {
            return current;
        }

        var minimal = Math.Min(packetTime, flushTime);
        if (minimal >= _interval)
        {
            minimal = (int)_interval;
        }

        return current + (uint)minimal;
    }

    public int SetMtu(int mtu)
    {
        ThrowIfDisposed();

        if (mtu <= Overhead)
        {
            return -1;
        }

        _mtu = (uint)mtu;
        _mss = _mtu - Overhead;
        _buffer = new byte[(mtu + Overhead) * 3];
        return 0;
    }

    public int NoDelay(int nodelay, int interval, int resend, int nc)
    {
        ThrowIfDisposed();

        if (nodelay >= 0)
        {
            _noDelay = (uint)nodelay;
            _rxMinRto = nodelay != 0 ? (uint)RtoNoDelay : RtoMin;
        }

        if (interval >= 0)
        {
            interval = Math.Clamp(interval, 10, 5000);
            _interval = (uint)interval;
        }

        if (resend >= 0)
        {
            _fastResend = resend;
        }

        if (nc >= 0)
        {
            _noCongestionWindow = nc;
        }

        return 0;
    }

    public int WindowSize(int sendWindow, int receiveWindow)
    {
        ThrowIfDisposed();

        if (sendWindow > 0)
        {
            _sendWindow = (uint)sendWindow;
        }

        if (receiveWindow > 0)
        {
            _receiveWindow = (uint)receiveWindow;
        }

        return 0;
    }

    public int WaitSnd()
    {
        ThrowIfDisposed();
        return _sendBuffer.Count + _sendQueue.Count;
    }

    public KcpDiagnostics GetDiagnostics()
    {
        ThrowIfDisposed();
        return new KcpDiagnostics(
            _inputDatagrams,
            _inputBytes,
            _inputAckSegments,
            _inputPushSegments,
            _queuedAckSegments,
            _outputDatagrams,
            _outputBytes,
            _outputAckSegments,
            _outputPushSegments,
            _outputInitialPushSegments,
            _outputFastResendPushSegments,
            _outputEarlyResendPushSegments,
            _outputRtoResendPushSegments,
            _selectiveAckedSegments,
            _cumulativeAckedSegments,
            _fastAckMarks,
            _fullFlushCount,
            _pendingFlushCount,
            _ackOnlyFlushCount,
            _sendQueue.Count,
            _sendBuffer.Count,
            _receiveQueue.Count,
            _receiveBuffer.Count,
            _ackList.Count,
            _sndUna,
            _sndNext,
            _rcvNext,
            _remoteWindow,
            _rxRto,
            _rxSrtt,
            _rxRttVar);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _sendQueue.Clear(ReleaseSegment);
        _receiveQueue.Clear(ReleaseSegment);
        _sendBuffer.Clear(ReleaseSegment);
        _receiveBuffer.Clear(ReleaseSegment);
        _ackList.Clear();
    }

    private static uint CurrentMs()
    {
        var elapsed = Stopwatch.GetTimestamp() - RefTimestamp;
        return unchecked((uint)(elapsed * TimestampToMilliseconds));
    }

    private static int Timediff(uint later, uint earlier)
    {
        return unchecked((int)(later - earlier));
    }

    private KcpSegment NewSegment(int size)
    {
        var capacity = Math.Max(Math.Max(size, (int)_mss), 1);
        KcpSegment segment;
        if (_segmentPool.Count > 0)
        {
            segment = _segmentPool.Pop();
        }
        else
        {
            segment = new KcpSegment();
        }
        segment.Reset();
        segment.Buffer = BufferPool.Rent(capacity);
        segment.Length = size;
        return segment;
    }

    private void RecycleSegmentData(KcpSegment segment)
    {
        if (segment.Buffer is null)
        {
            segment.Length = 0;
            return;
        }

        BufferPool.Return(segment.Buffer);
        segment.Buffer = null;
        segment.Length = 0;
    }

    private void ReleaseSegment(KcpSegment segment)
    {
        RecycleSegmentData(segment);
        segment.Reset();
        _segmentPool.Push(segment);
    }

    private ushort WindowUnused()
    {
        var receiveQueueCount = _receiveQueue.Count;
        return receiveQueueCount < (int)_receiveWindow
            ? (ushort)((int)_receiveWindow - receiveQueueCount)
            : (ushort)0;
    }

    private void UpdateAck(int rtt)
    {
        if (_rxSrtt == 0)
        {
            _rxSrtt = rtt;
            _rxRttVar = rtt >> 1;
        }
        else
        {
            var delta = rtt - _rxSrtt;
            _rxSrtt += delta >> 3;
            if (delta < 0)
            {
                delta = -delta;
            }

            if (rtt < _rxSrtt - _rxRttVar)
            {
                _rxRttVar += (delta - _rxRttVar) >> 5;
            }
            else
            {
                _rxRttVar += (delta - _rxRttVar) >> 2;
            }
        }

        var rto = (uint)_rxSrtt + Math.Max(_interval, (uint)_rxRttVar << 2);
        _rxRto = Math.Min(Math.Max(_rxMinRto, rto), RtoMax);
    }

    private void ShrinkBuffer()
    {
        var segment = _sendBuffer.Peek();
        _sndUna = segment is not null ? segment.Sn : _sndNext;
    }

    private void ParseAck(uint sn)
    {
        if (!TryGetSendBufferIndex(sn, out var index))
        {
            return;
        }

        var segment = _sendBuffer[index];
        if (!segment.Acked)
        {
            _selectiveAckedSegments++;
        }

        segment.Acked = true;
        segment.FastResendPending = false;
        RecycleSegmentData(segment);
    }

    private int ParseFastAck(uint sn, uint timestamp)
    {
        if (_fastResend <= 0 || !TryGetSendBufferIndex(sn, out var ackIndex))
        {
            return 0;
        }

        var shouldFastAck = 0;
        var resent = (uint)_fastResend;
        for (var i = 0; i < ackIndex; i++)
        {
            var segment = _sendBuffer[i];
            if (segment.Acked || segment.FastAck == uint.MaxValue)
            {
                continue;
            }

            if (Timediff(segment.Timestamp, timestamp) <= 0)
            {
                segment.FastAck++;
                _fastAckMarks++;
                if (segment.FastAck >= resent)
                {
                    segment.FastAck = uint.MaxValue;
                    segment.FastResendPending = true;
                    shouldFastAck = 1;
                }
            }
        }

        return shouldFastAck;
    }

    private int ParseAckRanges(ReadOnlySpan<byte> payload, out uint latestTimestamp)
    {
        latestTimestamp = 0;
        if (payload.Length < sizeof(ushort))
        {
            return -2;
        }

        var rangeCount = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        payload = payload[sizeof(ushort)..];
        if (payload.Length != rangeCount * AckRangePayloadSize)
        {
            return -2;
        }

        var shouldFastAck = 0;
        for (var i = 0; i < rangeCount; i++)
        {
            var start = BinaryPrimitives.ReadUInt32LittleEndian(payload);
            var end = BinaryPrimitives.ReadUInt32LittleEndian(payload[4..]);
            var timestamp = BinaryPrimitives.ReadUInt32LittleEndian(payload[8..]);
            payload = payload[AckRangePayloadSize..];

            if (Timediff(end, start) < 0)
            {
                return -2;
            }

            shouldFastAck |= ParseFastAckRange(start, end, timestamp);

            var sn = start;
            while (true)
            {
                ParseAck(sn);
                if (sn == end)
                {
                    break;
                }

                sn++;
            }

            latestTimestamp = timestamp;
        }

        return shouldFastAck;
    }

    private int ParseFastAckRange(uint start, uint end, uint timestamp)
    {
        if (_fastResend <= 0)
        {
            return 0;
        }

        var sendBufferCount = _sendBuffer.Count;
        var first = _sendBuffer.Peek();
        if (sendBufferCount == 0 || first is null || Timediff(start, first.Sn) <= 0)
        {
            return 0;
        }

        var bound = unchecked(start - first.Sn);
        var scanCount = bound >= (uint)sendBufferCount ? sendBufferCount : (int)bound;
        var rangeLength = unchecked(end - start) + 1;
        var incrementLimit = Math.Min((uint)_fastResend, rangeLength);
        var resent = (uint)_fastResend;
        var shouldFastAck = 0;

        for (var i = 0; i < scanCount; i++)
        {
            var segment = _sendBuffer[i];
            if (segment.Acked || segment.FastAck == uint.MaxValue)
            {
                continue;
            }

            if (Timediff(segment.Timestamp, timestamp) <= 0)
            {
                var need = resent > segment.FastAck ? resent - segment.FastAck : 0;
                var increment = Math.Min(need, incrementLimit);
                if (increment == 0)
                {
                    continue;
                }

                segment.FastAck += increment;
                _fastAckMarks += increment;
                if (segment.FastAck >= resent)
                {
                    segment.FastAck = uint.MaxValue;
                    segment.FastResendPending = true;
                    shouldFastAck = 1;
                }
            }
        }

        return shouldFastAck;
    }

    private bool TryGetSendBufferIndex(uint sn, out int index)
    {
        index = 0;
        var count = _sendBuffer.Count;
        if (count == 0 || Timediff(sn, _sndUna) < 0 || Timediff(sn, _sndNext) >= 0)
        {
            return false;
        }

        var first = _sendBuffer.Peek();
        if (first is null)
        {
            return false;
        }

        var offset = unchecked(sn - first.Sn);
        if (offset >= (uint)count)
        {
            return false;
        }

        index = (int)offset;
        return _sendBuffer[index].Sn == sn;
    }

    private int ParseUna(uint una)
    {
        var count = 0;
        var sendBufferCount = _sendBuffer.Count;
        for (var i = 0; i < sendBufferCount; i++)
        {
            var segment = _sendBuffer[i];
            if (Timediff(una, segment.Sn) <= 0)
            {
                break;
            }

            ReleaseSegment(segment);
            count++;
        }

        _sendBuffer.Discard(count);
        _cumulativeAckedSegments += (ulong)count;
        return count;
    }

    private void AckPush(uint sn, uint timestamp)
    {
        _ackList.Add(new AckItem(sn, timestamp));
        _queuedAckSegments++;
    }

    private bool ParseData(
        uint conv,
        byte cmd,
        byte fragment,
        ushort window,
        uint timestamp,
        uint sn,
        uint una,
        ReadOnlySpan<byte> payload)
    {
        if (Timediff(sn, _rcvNext + _receiveWindow) >= 0 || Timediff(sn, _rcvNext) < 0)
        {
            return true;
        }

        var repeat = false;
        if (sn == _rcvNext && _receiveQueue.Count < (int)_receiveWindow)
        {
            var segment = NewSegment(payload.Length);
            segment.Conv = conv;
            segment.Command = cmd;
            segment.Fragment = fragment;
            segment.Window = window;
            segment.Timestamp = timestamp;
            segment.Sn = sn;
            segment.Una = una;
            payload.CopyTo(segment.Buffer!.AsSpan(0, segment.Length));
            _receiveQueue.Push(segment);
            _rcvNext++;
            MoveReceiveBufferToQueue();
        }
        else if (!_receiveBuffer.Has(sn))
        {
            var segment = NewSegment(payload.Length);
            segment.Conv = conv;
            segment.Command = cmd;
            segment.Fragment = fragment;
            segment.Window = window;
            segment.Timestamp = timestamp;
            segment.Sn = sn;
            segment.Una = una;
            payload.CopyTo(segment.Buffer!.AsSpan(0, segment.Length));
            _receiveBuffer.Push(segment);
        }
        else
        {
            repeat = true;
        }

        MoveReceiveBufferToQueue();
        return repeat;
    }

    private void MoveReceiveBufferToQueue()
    {
        while (_receiveBuffer.Count > 0)
        {
            var segment = _receiveBuffer.Peek();
            if (segment is null || segment.Sn != _rcvNext || _receiveQueue.Count >= (int)_receiveWindow)
            {
                break;
            }

            _receiveQueue.Push(_receiveBuffer.Pop());
            _rcvNext++;
        }
    }

    private uint FlushCore(uint flushType)
    {
        var current = CurrentMs();
        if (flushType == FlushFull)
        {
            _fullFlushCount++;
        }
        else if (flushType == FlushPendingType)
        {
            _pendingFlushCount++;
        }
        else if (flushType == FlushAckOnly)
        {
            _ackOnlyFlushCount++;
        }

        var header = _flushHeader;
        header.Reset();
        header.Conv = _conv;
        header.Command = CommandAck;
        header.Window = WindowUnused();
        header.Una = _rcvNext;

        var offset = 0;

        void FlushBuffer()
        {
            if (offset <= 0)
            {
                return;
            }

            _output(_buffer.AsMemory(0, offset));
            _outputDatagrams++;
            _outputBytes += (ulong)offset;
            offset = 0;
        }

        void MakeSpace(int space)
        {
            if (offset + space > _mtu)
            {
                FlushBuffer();
            }
        }

        void SendSegment(KcpSegment segment, KcpSendReason sendReason)
        {
            var sendTime = CurrentMs();
            segment.Transmit++;
            segment.Timestamp = sendTime;
            segment.Window = header.Window;
            segment.Una = header.Una;

            if (segment.Transmit == 1)
            {
                segment.Rto = _rxRto;
                segment.ResendTimestamp = current + segment.Rto;
            }

            var need = Overhead + segment.Length;
            MakeSpace(need);
            EncodeSegment(segment, _buffer.AsSpan(offset, Overhead));
            offset += Overhead;
            segment.Buffer!.AsSpan(0, segment.Length).CopyTo(_buffer.AsSpan(offset, segment.Length));
            offset += segment.Length;
            CountKcpPushOutput(sendReason);
        }

        try
        {
            if (flushType is FlushAckOnly or FlushFull or FlushPendingType)
            {
                var ackRangeSent = TrySendAckRanges();
                if (!ackRangeSent)
                {
                    var lastAckIndex = _ackList.Count - 1;
                    for (var i = 0; i < _ackList.Count; i++)
                    {
                        var ack = _ackList[i];
                        MakeSpace(Overhead);
                        if (Timediff(ack.Sn, _rcvNext) >= 0 || i == lastAckIndex)
                        {
                            header.Command = CommandAck;
                            header.Sn = ack.Sn;
                            header.Timestamp = ack.Timestamp;
                            header.Length = 0;
                            EncodeSegment(header, _buffer.AsSpan(offset, Overhead));
                            offset += Overhead;
                            _outputAckSegments++;
                        }
                    }
                }

                _ackList.Clear();
            }

            if (_remoteWindow == 0)
            {
                if (_probeWait == 0)
                {
                    _probeWait = ProbeInitial;
                    _probeTimestamp = current + _probeWait;
                }
                else if (Timediff(current, _probeTimestamp) >= 0)
                {
                    _probeWait = Math.Max(_probeWait, ProbeInitial);
                    _probeWait += _probeWait / 2;
                    if (_probeWait > ProbeLimit)
                    {
                        _probeWait = ProbeLimit;
                    }

                    _probeTimestamp = current + _probeWait;
                    _probe |= AskSend;
                }
            }
            else
            {
                _probeTimestamp = 0;
                _probeWait = 0;
            }

            if ((_probe & AskSend) != 0)
            {
                header.Command = CommandWindowAsk;
                MakeSpace(Overhead);
                EncodeSegment(header, _buffer.AsSpan(offset, Overhead));
                offset += Overhead;
            }

            if ((_probe & AskTell) != 0)
            {
                header.Command = CommandWindowSize;
                MakeSpace(Overhead);
                EncodeSegment(header, _buffer.AsSpan(offset, Overhead));
                offset += Overhead;
            }

            _probe = 0;

            var congestionWindow = Math.Min(_sendWindow, _remoteWindow);
            if (_noCongestionWindow == 0)
            {
                congestionWindow = Math.Min(_congestionWindow, congestionWindow);
            }

            var newSegmentsCount = 0;
            while (Timediff(_sndNext, _sndUna + congestionWindow) < 0 && _sendQueue.Pop(out var newSegment))
            {
                newSegment!.Conv = _conv;
                newSegment.Command = CommandPush;
                newSegment.Sn = _sndNext;
                _sendBuffer.Push(newSegment);
                _sndNext++;
                newSegmentsCount++;

                if (flushType == FlushPendingType)
                {
                    SendSegment(newSegment, KcpSendReason.Initial);
                }
            }

            var resent = _fastResend > 0 ? (uint)_fastResend : uint.MaxValue;
            uint nextUpdate = _interval;
            ulong change = 0;
            ulong lostSegments = 0;

            if (flushType == FlushFull)
            {
                var sendBufferCount = _sendBuffer.Count;
                for (var i = 0; i < sendBufferCount; i++)
                {
                    var segment = _sendBuffer[i];
                    if (segment.Acked)
                    {
                        continue;
                    }

                    var needSend = false;
                    var sendReason = KcpSendReason.None;
                    if (segment.Transmit == 0)
                    {
                        needSend = true;
                        sendReason = KcpSendReason.Initial;
                        segment.Rto = _rxRto;
                        segment.ResendTimestamp = current + segment.Rto;
                    }
                    else if (segment.FastResendPending || (segment.FastAck >= resent && segment.FastAck != uint.MaxValue))
                    {
                        needSend = true;
                        sendReason = KcpSendReason.FastResend;
                        segment.FastResendPending = false;
                        segment.FastAck = uint.MaxValue;
                        segment.Rto = _rxRto;
                        segment.ResendTimestamp = current + segment.Rto;
                        change++;
                    }
                    else if (_fastResend > 0 && segment.FastAck > 0 && segment.FastAck != uint.MaxValue && newSegmentsCount == 0)
                    {
                        needSend = true;
                        sendReason = KcpSendReason.EarlyResend;
                        segment.FastAck = uint.MaxValue;
                        segment.Rto = _rxRto;
                        segment.ResendTimestamp = current + segment.Rto;
                        change++;
                    }
                    else if (Timediff(current, segment.ResendTimestamp) >= 0)
                    {
                        needSend = true;
                        sendReason = KcpSendReason.RtoResend;
                        segment.Rto += _noDelay == 0 ? _rxRto : _rxRto / 2;
                        segment.FastResendPending = false;
                        segment.FastAck = 0;
                        segment.ResendTimestamp = current + segment.Rto;
                        lostSegments++;
                    }

                    if (needSend)
                    {
                        SendSegment(segment, sendReason);

                        if (segment.Transmit >= _deadLink)
                        {
                            _state = uint.MaxValue;
                        }
                    }

                    var rto = Timediff(segment.ResendTimestamp, current);
                    if (rto > 0 && rto < nextUpdate)
                    {
                        nextUpdate = (uint)rto;
                    }
                }
            }

            if (_noCongestionWindow == 0)
            {
                if (change > 0)
                {
                    var inflight = _sndNext - _sndUna;
                    _ssthresh = Math.Max(inflight / 2, MinimumThreshold);
                    _congestionWindow = _ssthresh + resent;
                    _increment = _congestionWindow * _mss;
                }

                if (lostSegments > 0)
                {
                    _ssthresh = Math.Max(congestionWindow / 2, MinimumThreshold);
                    _congestionWindow = 1;
                    _increment = _mss;
                }

                if (_congestionWindow < 1)
                {
                    _congestionWindow = 1;
                    _increment = _mss;
                }
            }

            return nextUpdate;
        }
        finally
        {
            FlushBuffer();
        }

        bool TrySendAckRanges()
        {
            if (_ackList.Count <= 1)
            {
                return false;
            }

            var lastAck = _ackList[^1];
            if (!IsAckListSorted())
            {
                _ackList.Sort(static (left, right) => Timediff(left.Sn, right.Sn));
            }

            CountAckStats(lastAck.Sn, out var oldAckCount, out var rangeCount);
            if (oldAckCount <= 1 || rangeCount <= 0)
            {
                return false;
            }

            var payloadLength = sizeof(ushort) + (rangeCount * AckRangePayloadSize);
            if (Overhead + payloadLength >= oldAckCount * Overhead)
            {
                return false;
            }

            MakeSpace(Overhead + payloadLength);
            header.Command = CommandAckRange;
            header.Sn = lastAck.Sn;
            header.Timestamp = lastAck.Timestamp;
            header.Length = payloadLength;
            EncodeSegment(header, _buffer.AsSpan(offset, Overhead));
            offset += Overhead;

            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(offset, sizeof(ushort)), (ushort)rangeCount);
            offset += sizeof(ushort);
            WriteAckRanges(lastAck.Sn);
            _outputAckSegments++;

            header.Command = CommandAck;
            header.Length = 0;
            return true;
        }

        bool IsAckListSorted()
        {
            for (var i = 1; i < _ackList.Count; i++)
            {
                if (Timediff(_ackList[i].Sn, _ackList[i - 1].Sn) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        void CountAckStats(uint lastSn, out int wireAckCount, out int rangeCount)
        {
            wireAckCount = 0;
            rangeCount = 0;
            var haveRange = false;
            uint end = 0;
            for (var i = 0; i < _ackList.Count; i++)
            {
                var ack = _ackList[i];
                if (Timediff(ack.Sn, _rcvNext) < 0 && ack.Sn != lastSn)
                {
                    continue;
                }

                wireAckCount++;
                if (!haveRange)
                {
                    rangeCount++;
                    end = ack.Sn;
                    haveRange = true;
                    continue;
                }

                if (ack.Sn == end)
                {
                    continue;
                }

                if (ack.Sn == unchecked(end + 1))
                {
                    end = ack.Sn;
                    continue;
                }

                rangeCount++;
                end = ack.Sn;
            }
        }

        void WriteAckRanges(uint lastSn)
        {
            var haveRange = false;
            uint start = 0;
            uint end = 0;
            uint endTimestamp = 0;

            for (var i = 0; i < _ackList.Count; i++)
            {
                var ack = _ackList[i];
                if (Timediff(ack.Sn, _rcvNext) < 0 && ack.Sn != lastSn)
                {
                    continue;
                }

                if (!haveRange)
                {
                    start = ack.Sn;
                    end = ack.Sn;
                    endTimestamp = ack.Timestamp;
                    haveRange = true;
                    continue;
                }

                if (ack.Sn == end)
                {
                    endTimestamp = ack.Timestamp;
                    continue;
                }

                if (ack.Sn == unchecked(end + 1))
                {
                    end = ack.Sn;
                    endTimestamp = ack.Timestamp;
                    continue;
                }

                WriteAckRange(start, end, endTimestamp);
                start = ack.Sn;
                end = ack.Sn;
                endTimestamp = ack.Timestamp;
            }

            if (haveRange)
            {
                WriteAckRange(start, end, endTimestamp);
            }
        }

        void WriteAckRange(uint start, uint end, uint timestamp)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(offset, sizeof(uint)), start);
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(offset + 4, sizeof(uint)), end);
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(offset + 8, sizeof(uint)), timestamp);
            offset += AckRangePayloadSize;
        }
    }

    private void CountKcpPushOutput(KcpSendReason reason)
    {
        _outputPushSegments++;
        switch (reason)
        {
            case KcpSendReason.Initial:
                _outputInitialPushSegments++;
                break;
            case KcpSendReason.FastResend:
                _outputFastResendPushSegments++;
                break;
            case KcpSendReason.EarlyResend:
                _outputEarlyResendPushSegments++;
                break;
            case KcpSendReason.RtoResend:
                _outputRtoResendPushSegments++;
                break;
        }
    }

    private static void EncodeSegment(KcpSegment segment, Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(destination, segment.Conv);
        destination[4] = segment.Command;
        destination[5] = segment.Fragment;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[6..], segment.Window);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], segment.Timestamp);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], segment.Sn);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[16..], segment.Una);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[20..], (uint)segment.Length);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private readonly record struct AckItem(uint Sn, uint Timestamp);

    private enum KcpSendReason
    {
        None,
        Initial,
        FastResend,
        EarlyResend,
        RtoResend
    }
}
