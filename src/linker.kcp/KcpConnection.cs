using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace linker.kcp;

public delegate void KcpReceiveRecordHandler(ReadOnlySpan<byte> payload);

public sealed class KcpConnection : IAsyncDisposable
{
    private readonly object _syncRoot = new();
    private readonly Socket _udpSocket;
    private readonly EndPoint _remoteEndpoint;
    private readonly Kcp _kcp;
    private readonly SpscReceiveBuffer _receiveBuffer;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly SemaphoreSlim _sendSignal = new(0, 1);
    private readonly int _mtu;
    private readonly int _maxPendingSegments;
    private readonly int _flushBatchSegments;
    private readonly int _ackFlushBatchPackets;
    private readonly Task _updateTask;
    private readonly Task? _receiveTask;

    private int _sendSignalSet;
    private int _disposeStarted;
    private int _sendSinceFlush;
    private int _inputSinceFlush;
    private long _sendWouldBlockCount;
    private long _sendDropCount;
    private long _waitSndPeak;

    public KcpConnection(
        uint conv,
        int mtu,
        int window,
        int nodelay,
        int interval,
        int resend,
        int nc,
        Socket udpSocket,
        EndPoint remoteEndpoint,
        bool recv = true,
        int flushBatchSegments = 128,
        int ackFlushBatchPackets = 1024)
    {
        if (mtu <= Kcp.Overhead)
        {
            throw new ArgumentOutOfRangeException(nameof(mtu), "MTU must be greater than the KCP header size.");
        }

        if (window <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be greater than zero.");
        }

        _udpSocket = udpSocket ?? throw new ArgumentNullException(nameof(udpSocket));
        _remoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
        _mtu = mtu;
        _maxPendingSegments = window;
        _flushBatchSegments = Math.Clamp(flushBatchSegments, 1, window);
        _ackFlushBatchPackets = Math.Clamp(ackFlushBatchPackets, 1, window);

        TrySetNonBlocking(_udpSocket);

        _kcp = new Kcp(conv, SendKcpPacket);
        if (_kcp.SetMtu(mtu) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mtu), "Invalid KCP MTU.");
        }

        _kcp.WindowSize(window, window);
        _kcp.NoDelay(nodelay, interval, resend, nc);
        _receiveBuffer = new SpscReceiveBuffer(Math.Max((mtu + sizeof(ushort)) * 64, 64 * 1024));

        _updateTask = Task.Run(UpdateLoopAsync);
        _receiveTask = recv ? StartLongRunning(ReceiveLoop) : null;
    }

    public int WaitSnd
    {
        get
        {
            lock (_syncRoot)
            {
                var waitSnd = _kcp.WaitSnd();
                UpdateWaitSndPeak(waitSnd);
                return waitSnd;
            }
        }
    }

    public long SendWouldBlockCount => Interlocked.Read(ref _sendWouldBlockCount);

    public long SendDropCount => Interlocked.Read(ref _sendDropCount);

    public long WaitSndPeak => Interlocked.Read(ref _waitSndPeak);

    public void Flush()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            _kcp.Flush();
            _sendSinceFlush = 0;
            _inputSinceFlush = 0;
        }
    }

    public void FlushPending()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            _kcp.FlushPending();
            _sendSinceFlush = 0;
            _inputSinceFlush = 0;
        }
    }

    public KcpDiagnostics GetDiagnostics()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            return _kcp.GetDiagnostics();
        }
    }

    public void Input(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        if ((uint)offset > (uint)data.Length || (uint)length > (uint)(data.Length - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        Input(data.AsMemory(offset, length));
    }

    public void Input(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            _kcp.Input(data.Span, KcpPacketType.Regular, ackNoDelay: false);
            _inputSinceFlush++;
            if (_inputSinceFlush >= _ackFlushBatchPackets)
            {
                _kcp.FlushPending();
                _inputSinceFlush = 0;
                _sendSinceFlush = 0;
            }

            DrainReceiveLocked();
            SignalSendCapacityIfAvailableLocked();
        }
    }

    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (data.IsEmpty)
        {
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }

        await SendAsyncCore(data, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length < sizeof(ushort))
        {
            throw new ArgumentException("The receive buffer must have room for at least a 2-byte length header.", nameof(buffer));
        }

        return await _receiveBuffer.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<int> ReceiveRecordsAsync(KcpReceiveRecordHandler handler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return await _receiveBuffer.ReadRecordsAsync(handler, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        _disposeCts.Cancel();
        SignalSendCapacity();
        _receiveBuffer.Complete();

        if (_receiveTask is not null)
        {
            await IgnoreCancellationAsync(_receiveTask).ConfigureAwait(false);
        }

        await IgnoreCancellationAsync(_updateTask).ConfigureAwait(false);

        lock (_syncRoot)
        {
            _kcp.Dispose();
        }

        _receiveBuffer.Dispose();
        _disposeCts.Dispose();
        _sendSignal.Dispose();
    }

    private async ValueTask SendAsyncCore(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (data.Length > ushort.MaxValue)
                {
                    throw new ArgumentException("The payload is too large for the 2-byte receive record prefix.", nameof(data));
                }

                var waitSnd = _kcp.WaitSnd();
                UpdateWaitSndPeak(waitSnd);

                if (waitSnd < _maxPendingSegments)
                {
                    var result = _kcp.Send(data.Span);
                    if (result == 0)
                    {
                        _sendSinceFlush++;
                        if (_sendSinceFlush >= _flushBatchSegments)
                        {
                            _kcp.FlushPending();
                            _sendSinceFlush = 0;
                            _inputSinceFlush = 0;
                        }

                        return;
                    }

                    throw new ArgumentException("The payload is empty or too large for one KCP message.", nameof(data));
                }
            }

            await WaitSendCapacityAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void ReceiveLoop()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(_mtu);
        var remote = CreateAnyEndpoint(_udpSocket.AddressFamily);
        var token = _disposeCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var received = _udpSocket.ReceiveFrom(buffer, SocketFlags.None, ref remote);
                    if (received > 0)
                    {
                        Input(buffer.AsMemory(0, received));
                    }
                }
                catch (SocketException ex) when (IsWouldBlock(ex))
                {
                    PollSocket(_udpSocket, SelectMode.SelectRead);
                }
                catch (SocketException ex) when (IsExpectedReceiveException(ex))
                {
                }
            }
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task UpdateLoopAsync()
    {
        var token = _disposeCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                int delay;
                lock (_syncRoot)
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    _kcp.Update();
                    _sendSinceFlush = 0;
                    _inputSinceFlush = 0;
                    DrainReceiveLocked();
                    SignalSendCapacityIfAvailableLocked();
                    delay = Math.Clamp(_kcp.Interval, 1, 100);
                }

                await Task.Delay(delay, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private void DrainReceiveLocked()
    {
        while (true)
        {
            var size = _kcp.PeekSize();
            if (size <= 0)
            {
                break;
            }

            var received = _receiveBuffer.WriteRecordFromKcp(_kcp, size);
            if (received <= 0)
            {
                break;
            }

            SignalSendCapacityIfAvailableLocked();
        }
    }

    private void SendKcpPacket(ReadOnlyMemory<byte> packet)
    {
        try
        {
            _udpSocket.SendTo(packet.Span, SocketFlags.None, _remoteEndpoint);
        }
        catch (SocketException ex) when (IsWouldBlock(ex))
        {
            Interlocked.Increment(ref _sendWouldBlockCount);
            Interlocked.Increment(ref _sendDropCount);
        }
        catch (SocketException ex) when (IsExpectedSendException(ex))
        {
            Interlocked.Increment(ref _sendDropCount);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void UpdateWaitSndPeak(int count)
    {
        while (true)
        {
            var current = Interlocked.Read(ref _waitSndPeak);
            if (count <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _waitSndPeak, count, current) == current)
            {
                return;
            }
        }
    }

    private void SignalSendCapacityIfAvailableLocked()
    {
        if (_kcp.WaitSnd() < _maxPendingSegments)
        {
            SignalSendCapacity();
        }
    }

    private void SignalSendCapacity()
    {
        if (Interlocked.Exchange(ref _sendSignalSet, 1) == 0)
        {
            _sendSignal.Release();
        }
    }

    private async ValueTask WaitSendCapacityAsync(CancellationToken cancellationToken)
    {
        await _sendSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Exchange(ref _sendSignalSet, 0);
    }

    private bool IsDisposed => Volatile.Read(ref _disposeStarted) != 0;

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
    }

    private static EndPoint CreateAnyEndpoint(AddressFamily addressFamily)
    {
        return addressFamily == AddressFamily.InterNetworkV6
            ? new IPEndPoint(IPAddress.IPv6Any, 0)
            : new IPEndPoint(IPAddress.Any, 0);
    }

    private static void TrySetNonBlocking(Socket socket)
    {
        try
        {
            socket.Blocking = false;
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static bool IsExpectedReceiveException(SocketException ex)
    {
        return ex.SocketErrorCode is SocketError.TimedOut
            or SocketError.WouldBlock
            or SocketError.Interrupted
            or SocketError.OperationAborted
            or SocketError.ConnectionReset;
    }

    private static bool IsWouldBlock(SocketException ex)
    {
        return ex.SocketErrorCode is SocketError.WouldBlock or SocketError.IOPending or SocketError.NoBufferSpaceAvailable;
    }

    private static bool IsExpectedSendException(SocketException ex)
    {
        return ex.SocketErrorCode is SocketError.AccessDenied
            or SocketError.NoBufferSpaceAvailable
            or SocketError.WouldBlock
            or SocketError.ConnectionReset
            || ex.NativeErrorCode is 1 or 13;
    }

    private static void PollSocket(Socket socket, SelectMode mode)
    {
        try
        {
            socket.Poll(1000, mode);
        }
        catch (SocketException ex) when (IsExpectedPollException(ex))
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static bool IsExpectedPollException(SocketException ex)
    {
        return ex.SocketErrorCode is SocketError.TimedOut
            or SocketError.WouldBlock
            or SocketError.Interrupted
            or SocketError.OperationAborted
            or SocketError.ConnectionReset
            or SocketError.NotSocket
            || ex.NativeErrorCode is 1 or 13 or 10004;
    }

    private static async ValueTask IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static Task StartLongRunning(Action action)
    {
        return Task.Factory.StartNew(
            action,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }
}
