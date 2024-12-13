using linker.config;
using linker.libs;
using linker.libs.extends;
using linker.plugins.relay.server.caching;
using linker.plugins.resolver;
using linker.plugins.server;
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
        private readonly RelayServerConfigTransfer relayServerConfigTransfer;
        private readonly ServerConfigTransfer serverConfigTransfer;

        private readonly ICrypto cryptoNode;
        private readonly ICrypto cryptoMaster;

        private ulong bytes = 0;
        private ulong lastBytes = 0;

        RelaySpeedLimit limitTotal = new RelaySpeedLimit();

        public RelayServerNodeTransfer(IRelayCaching relayCaching, FileConfig fileConfig, RelayServerConfigTransfer relayServerConfigTransfer, ServerConfigTransfer serverConfigTransfer)
        {
            this.relayCaching = relayCaching;
            this.fileConfig = fileConfig;
            this.relayServerConfigTransfer = relayServerConfigTransfer;
            this.serverConfigTransfer = serverConfigTransfer;

            limitTotal.SetLimit((uint)Math.Ceiling((relayServerConfigTransfer.Node.MaxBandwidthTotal * 1024 * 1024) / 8.0));

            cryptoNode = CryptoFactory.CreateSymmetric(relayServerConfigTransfer.Node.MasterSecretKey);
            cryptoMaster = CryptoFactory.CreateSymmetric(relayServerConfigTransfer.Master.SecretKey);
            ReportTask();
        }

        public async ValueTask<RelayCache> TryGetRelayCache(string key, string nodeid)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(2 * 1024);
            try
            {
                IPEndPoint server = nodeid == RelayNodeInfo.MASTER_NODE_ID
                    ? new IPEndPoint(IPAddress.Loopback, serverConfigTransfer.Port)
                    : await NetworkHelper.GetEndPointAsync(relayServerConfigTransfer.Node.MasterHost, 1802);
                ICrypto crypto = nodeid == RelayNodeInfo.MASTER_NODE_ID ? cryptoMaster : cryptoNode;

                Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                long start = Environment.TickCount64;
                await socket.ConnectAsync(server).ConfigureAwait(false);
                long time = Environment.TickCount64 - start;

                await socket.SendAsync(new byte[] { (byte)ResolverType.RelayReport });
                await socket.SendAsync(key.ToBytes());
                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(Math.Max(time * 2, 5000))).ConfigureAwait(false);
                socket.SafeClose();

                RelayCache result = MemoryPackSerializer.Deserialize<RelayCache>(crypto.Decode(buffer.AsMemory(0, length).ToArray()).Span);
                return result;
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

        /// <summary>
        /// 无效请求
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            return ValidateConnection() && ValidateBytes();
        }

        /// <summary>
        /// 增加连接数
        /// </summary>
        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }
        /// <summary>
        /// 减少连接数
        /// </summary>
        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }
        /// <summary>
        /// 连接数是否够
        /// </summary>
        /// <returns></returns>
        public bool ValidateConnection()
        {
            bool res = relayServerConfigTransfer.Node.MaxConnection == 0 || relayServerConfigTransfer.Node.MaxConnection * 2 > connectionNum;
            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateConnection false,{connectionNum}/{relayServerConfigTransfer.Node.MaxConnection * 2}");

            return res;
        }

        /// <summary>
        /// 流量是否够
        /// </summary>
        /// <returns></returns>
        public bool ValidateBytes()
        {
            bool res = relayServerConfigTransfer.Node.MaxGbTotal == 0
                || (relayServerConfigTransfer.Node.MaxGbTotal > 0 && relayServerConfigTransfer.Node.MaxGbTotalLastBytes > 0);

            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateBytes false,{relayServerConfigTransfer.Node.MaxGbTotalLastBytes}bytes/{relayServerConfigTransfer.Node.MaxGbTotal}gb");

            return res;
        }
        /// <summary>
        /// 添加流量
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool AddBytes(ulong length)
        {
            bytes += length;
            if (relayServerConfigTransfer.Node.MaxGbTotal == 0)
            {
                return true;
            }

            if (relayServerConfigTransfer.Node.MaxGbTotalLastBytes >= length)
                relayServerConfigTransfer.SetMaxGbTotalLastBytes(relayServerConfigTransfer.Node.MaxGbTotalLastBytes - length);
            else relayServerConfigTransfer.SetMaxGbTotalLastBytes(0);
            return relayServerConfigTransfer.Node.MaxGbTotalLastBytes > 0;
        }

        /// <summary>
        /// 获取单个限速
        /// </summary>
        /// <returns></returns>
        public uint GetBandwidthLimit()
        {
            return (uint)Math.Ceiling((relayServerConfigTransfer.Node.MaxBandwidth * 1024 * 1024) / 8.0);
        }
        /// <summary>
        /// 是否需要总限速
        /// </summary>
        /// <returns></returns>
        public bool NeedLimit()
        {
            return limitTotal.NeedLimit();
        }
        /// <summary>
        /// 总限速
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool TryLimit(ref int length)
        {
            return limitTotal.TryLimit(ref length);
        }


        private void ResetBytes()
        {
            if (relayServerConfigTransfer.Node.MaxGbTotalMonth != DateTime.Now.Month)
            {
                relayServerConfigTransfer.SetMaxGbTotalMonth(DateTime.Now.Month);
                relayServerConfigTransfer.SetMaxGbTotalLastBytes((ulong)(relayServerConfigTransfer.Node.MaxGbTotal * 1024 * 1024 * 1024));
            }
            relayServerConfigTransfer.Update();
        }

        private void ReportTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                ResetBytes();
                IEnumerable<RelayNodeInfo> nodes = new List<RelayNodeInfo>
                {
                    //默认报告给自己，作为本服务器的一个默认中继节点
                    new RelayNodeInfo{
                        Id = RelayNodeInfo.MASTER_NODE_ID,
                        Host = new IPEndPoint(IPAddress.Any,serverConfigTransfer.Port).ToString(),
                        MasterHost =  new IPEndPoint(IPAddress.Loopback,serverConfigTransfer.Port).ToString(),
                        MasterSecretKey = relayServerConfigTransfer.Master.SecretKey,
                        MaxBandwidth = 0,
                        MaxConnection = 0,
                        MaxBandwidthTotal=0,
                        MaxGbTotal=0,
                        MaxGbTotalLastBytes=0,
                        MaxGbTotalMonth=0,
                        Name = "default",
                        Public = false
                    },
                    //配置的中继节点
                    relayServerConfigTransfer.Node
                }.Where(c => string.IsNullOrWhiteSpace(c.MasterHost) == false && string.IsNullOrWhiteSpace(c.MasterSecretKey) == false)
                .Where(c => string.IsNullOrWhiteSpace(c.Name) == false && string.IsNullOrWhiteSpace(c.Id) == false);

                double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                lastBytes = bytes;

                foreach (var node in nodes)
                {
                    try
                    {
                        ICrypto crypto = node.Id == RelayNodeInfo.MASTER_NODE_ID ? cryptoMaster : cryptoNode;
                        IPEndPoint endPoint = await NetworkHelper.GetEndPointAsync(node.Host, serverConfigTransfer.Port) ?? new IPEndPoint(IPAddress.Any, serverConfigTransfer.Port);

                        RelayNodeReportInfo relayNodeReportInfo = new RelayNodeReportInfo
                        {
                            Id = node.Id,
                            Name = node.Name,
                            Public = node.Public,
                            MaxBandwidth = node.MaxBandwidth,
                            BandwidthRatio = Math.Round(node.MaxBandwidthTotal == 0 ? 0 : diff / 5 / node.MaxBandwidthTotal, 2),
                            MaxBandwidthTotal = node.MaxBandwidthTotal,
                            MaxGbTotal = node.MaxGbTotal,
                            MaxGbTotalLastBytes = node.MaxGbTotalLastBytes,
                            MaxConnection = node.MaxConnection,
                            ConnectionRatio = Math.Round(node.MaxConnection == 0 ? 0 : connectionNum / 2.0 / node.MaxConnection, 2),
                            EndPoint = endPoint,
                        };

                        IPEndPoint ep = await NetworkHelper.GetEndPointAsync(node.MasterHost, serverConfigTransfer.Port);

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
