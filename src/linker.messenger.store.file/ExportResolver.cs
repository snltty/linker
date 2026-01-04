
using linker.libs;
using linker.libs.extends;
using Microsoft.Extensions.Caching.Memory;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.store.file
{
    public sealed class ExportResolver : IResolver
    {
        public byte Type => 222;

        private readonly IMemoryCache cache = new MemoryCache(new MemoryCacheOptions { });

        private readonly ISerializer serializer;
        public ExportResolver(ISerializer serializer)
        {
            this.serializer = serializer;
        }

        public async Task<string> Save(string server, string value)
        {
            return await Save(NetworkHelper.GetEndPoint(server, 1802), value).ConfigureAwait(false);
        }
        public async Task<string> Save(IPEndPoint server, string value)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                await socket.ConnectAsync(server, cts.Token);

                buffer[0] = Type;
                await socket.SendAsync(buffer.AsMemory(0, 1));

                byte[] playload = serializer.Serialize(new ExportSaveInfo { Type = ExportSaveType.Save, Value = value }.ToJson());
                playload.Length.ToBytes().CopyTo(buffer.AsSpan(0, 4));
                playload.CopyTo(buffer.AsSpan(4));

                await socket.SendAsync(buffer.AsMemory(0, playload.Length + 4));

                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, cts.Token).ConfigureAwait(false);

                return serializer.Deserialize<string>(buffer.AsSpan(0, length));
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                socket.SafeClose();
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return string.Empty;
        }
        public async Task<string> Get(string server, string value)
        {
            return await Get(NetworkHelper.GetEndPoint(server, 1802), value).ConfigureAwait(false);
        }
        public async Task<string> Get(IPEndPoint server, string value)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            using CancellationTokenSource cts = new CancellationTokenSource(5000);
            try
            {
                await socket.ConnectAsync(server, cts.Token);

                buffer[0] = Type;
                await socket.SendAsync(buffer.AsMemory(0, 1));
                await socket.SendAsync(serializer.Serialize(new ExportSaveInfo { Type = ExportSaveType.Get, Value = value }.ToJson()));

                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None, cts.Token).ConfigureAwait(false);

                return serializer.Deserialize<string>(buffer.AsSpan(0, length));
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                socket.SafeClose();
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return string.Empty;
        }


        public async Task Resolve(Socket socket, Memory<byte> memory)
        {
            using CancellationTokenSource cts = new CancellationTokenSource(3000);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8 * 1024);
            try
            {
                int length = 0, payloadLength = 0;

                while (length < payloadLength + 4)
                {
                    length += await socket.ReceiveAsync(buffer.AsMemory(length), SocketFlags.None, cts.Token).ConfigureAwait(false);
                    if (length >= 4) payloadLength = buffer.ToInt32();
                }

                ExportSaveInfo info = serializer.Deserialize<string>(buffer.AsMemory(4, length - 4).Span).DeJson<ExportSaveInfo>();

                if (string.IsNullOrWhiteSpace(info.Value))
                {
                    socket.SafeClose();
                    return;
                }
                switch (info.Type)
                {
                    case ExportSaveType.Save:
                        {
                            string key = Guid.NewGuid().ToString();
                            cache.Set(key, info.Value, TimeSpan.FromMinutes(10));
                            await socket.SendAsync(serializer.Serialize(key));
                        }
                        break;
                    case ExportSaveType.Get:
                        {
                            if (cache.TryGetValue(info.Value, out string value))
                            {
                                cache.Remove(info.Value);
                                await socket.SendAsync(serializer.Serialize(value));
                            }
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                socket.SafeClose();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public async Task Resolve(Socket socket, IPEndPoint ep, Memory<byte> memory)
        {
            await Task.CompletedTask;
        }
    }

    public sealed class ExportSaveInfo
    {
        public ExportSaveType Type { get; set; } = ExportSaveType.Save;
        public string Value { get; set; } = string.Empty;
    }

    public enum ExportSaveType : byte
    {
        Save = 0,
        Get = 1,
    }
}
