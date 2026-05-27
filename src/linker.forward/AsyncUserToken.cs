using linker.libs;
using linker.libs.extends;
using linker.tunnel.connection;
using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace linker.forward
{
    public sealed class AsyncUserToken
    {
        public int ListenPort { get; set; }

        public TaskCompletionSource Tcs { get; set; }

        public Socket Socket { get; set; }
        public ITunnelConnection Connection { get; set; }
        public ForwardReadPacket ReadPacket { get; set; }

        public IPEndPoint IPEndPoint { get; set; }

        public LastTicksManager LastTicks { get; set; } = new LastTicksManager();
        public bool Timeout => LastTicks.Expired(60 * 1000);

        public Pipe Pipe { get; init; }
        private long received = 0;
        public long Received => received;

        public bool Sending { get; set; } = true;
        public bool Receiving { get; set; } = true;
        public void AddReceived(long value)
        {
            Interlocked.Add(ref received, value);
        }
        public bool NeedPause => Received > 512 * 1024 && Receiving;
        public bool NeedResume => Received < 128 * 1024 && Receiving == false;

        public void Dispose()
        {
            Pipe?.Writer.Complete();
            Pipe?.Reader.Complete();

            Socket?.SafeClose();
            Socket = null;

            ReadPacket?.Dispose();

            GC.Collect();
        }
    }
}
