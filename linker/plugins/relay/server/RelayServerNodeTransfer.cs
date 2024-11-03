using linker.config;
using linker.libs;
using linker.libs.extends;
using linker.plugins.relay.server.caching;
using linker.plugins.resolver;
using MemoryPack;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.plugins.relay.server
{
    public class RelayServerNodeTransfer
    {
        private uint connectionNum = 0;


        private readonly IRelayCaching relayCaching;
        private readonly FileConfig fileConfig;

        private readonly ICrypto crypto;

        public RelayServerNodeTransfer(IRelayCaching relayCaching, FileConfig fileConfig)
        {
            this.relayCaching = relayCaching;
            this.fileConfig = fileConfig;

            crypto = CryptoFactory.CreateSymmetric(fileConfig.Data.Server.Relay.Distributed.Node.MasterSecretKey);
            ReportTask();
        }

        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }
        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }
        public bool ValidateConnection()
        {
            return fileConfig.Data.Server.Relay.Distributed.Node.MaxConnection == 0 || fileConfig.Data.Server.Relay.Distributed.Node.MaxConnection * 2 > connectionNum;
        }

        public async ValueTask<RelayCache> TryGetRelayCache(string key)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(2 * 1024);
            try
            {
                IPEndPoint server = await NetworkHelper.GetEndPointAsync(fileConfig.Data.Server.Relay.Distributed.Node.MasterHost, 1802);


                Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                long start = Environment.TickCount64;
                await socket.ConnectAsync(server).ConfigureAwait(false);
                long time = Environment.TickCount64 - start;

                await socket.SendAsync(new byte[] { (byte)ResolverType.RelayReport });
                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(time * 2)).ConfigureAwait(false);
                socket.SafeClose();

                return MemoryPackSerializer.Deserialize<RelayCache>(crypto.Decode(buffer.AsMemory(0, length).ToArray()).Span);
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"{ex}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return null;
        }

        public uint GetBandwidthLimit()
        {
            return (uint)Math.Ceiling((fileConfig.Data.Server.Relay.Distributed.Node.MaxBandwidth * 1024 * 1024) / 8.0);
        }
        private void ReportTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                List<RelayNodeInfo> nodes = new List<RelayNodeInfo>
                {
                    new RelayNodeInfo{
                        Id=string.Empty,
                        Host=new IPEndPoint(IPAddress.Any,fileConfig.Data.Server.ServicePort).ToString(),
                        MasterHost =  new IPEndPoint(IPAddress.Loopback,fileConfig.Data.Server.ServicePort).ToString(),
                        MasterSecretKey = fileConfig.Data.Server.Relay.Distributed.Master.SecretKey,
                        MaxBandwidth = 0,
                        MaxConnection = 0,
                        Name = "default",
                        Public = false
                    },
                    fileConfig.Data.Server.Relay.Distributed.Node
                };
                foreach (var node in nodes.Where(c => string.IsNullOrWhiteSpace(c.MasterHost) == false && string.IsNullOrWhiteSpace(c.MasterSecretKey) == false))
                {
                    try
                    {
                        IPEndPoint endPoint = await NetworkHelper.GetEndPointAsync(node.Host, fileConfig.Data.Server.ServicePort) ?? new IPEndPoint(IPAddress.Any, fileConfig.Data.Server.ServicePort);

                        double connectionRatio = node.MaxConnection == 0 ? 0 : connectionNum / node.MaxConnection;
                        RelayNodeReportInfo relayNodeReportInfo = new RelayNodeReportInfo
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Public = node.Public,
                            BandwidthRatio = 0,
                            ConnectionRatio = Math.Round(connectionRatio, 2),
                            EndPoint = endPoint,
                        };

                        IPEndPoint ep = await NetworkHelper.GetEndPointAsync(node.MasterHost, fileConfig.Data.Server.ServicePort);

                        byte[] content = crypto.Encode(MemoryPackSerializer.Serialize(relayNodeReportInfo));
                        byte[] data = new byte[content.Length + 1];
                        data[0] = (byte)ResolverType.RelayReport;
                        content.AsMemory().CopyTo(data.AsMemory(1));

                        using UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
                        udpClient.Client.WindowsUdpBug();
                        await udpClient.SendAsync(data, ep);
                    }
                    catch (Exception)
                    {
                    }
                }


                return true;
            }, () => 5000);
        }
    }


}
