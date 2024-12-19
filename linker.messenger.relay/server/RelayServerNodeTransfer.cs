using linker.libs;
using linker.libs.extends;
using linker.messenger.relay.server.caching;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace linker.messenger.relay.server
{
    public class RelayServerNodeTransfer
    {
        private uint connectionNum = 0;

        private readonly ICrypto cryptoNode;
        private readonly ICrypto cryptoMaster;

        private ulong bytes = 0;
        private ulong lastBytes = 0;
        RelaySpeedLimit limitTotal = new RelaySpeedLimit();

        private readonly ISerializer serializer;
        private readonly IRelayServerCaching relayCaching;
        private readonly IRelayServerNodeStore relayServerNodeStore;
        private readonly IRelayServerMasterStore relayServerMasterStore;
        public RelayServerNodeTransfer(ISerializer serializer, IRelayServerCaching relayCaching, IRelayServerNodeStore relayServerNodeStore, IRelayServerMasterStore relayServerMasterStore)
        {
            this.serializer = serializer;
            this.relayCaching = relayCaching;
            this.relayServerNodeStore = relayServerNodeStore;
            this.relayServerMasterStore = relayServerMasterStore;

            limitTotal.SetLimit((uint)Math.Ceiling((relayServerNodeStore.Node.MaxBandwidthTotal * 1024 * 1024) / 8.0));

            cryptoNode = CryptoFactory.CreateSymmetric(relayServerNodeStore.Node.MasterSecretKey);
            cryptoMaster = CryptoFactory.CreateSymmetric(relayServerMasterStore.Master.SecretKey);
            ReportTask();
        }

        public async ValueTask<RelayCacheInfo> TryGetRelayCache(string key, string nodeid)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(2 * 1024);
            try
            {
                IPEndPoint server = nodeid == RelayServerNodeInfo.MASTER_NODE_ID
                    ? new IPEndPoint(IPAddress.Loopback, relayServerNodeStore.ServicePort)
                    : await NetworkHelper.GetEndPointAsync(relayServerNodeStore.Node.MasterHost, 1802);
                ICrypto crypto = nodeid == RelayServerNodeInfo.MASTER_NODE_ID ? cryptoMaster : cryptoNode;

                Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                long start = Environment.TickCount64;
                await socket.ConnectAsync(server).ConfigureAwait(false);
                long time = Environment.TickCount64 - start;

                if (relayServerNodeStore.Flag > 0)
                    await socket.SendAsync(new byte[] { relayServerNodeStore.Flag });
                await socket.SendAsync(key.ToBytes());
                int length = await socket.ReceiveAsync(buffer.AsMemory(), SocketFlags.None).AsTask().WaitAsync(TimeSpan.FromMilliseconds(Math.Max(time * 2, 5000))).ConfigureAwait(false);
                socket.SafeClose();

                RelayCacheInfo result = serializer.Deserialize<RelayCacheInfo>(crypto.Decode(buffer.AsMemory(0, length).ToArray()).Span);
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
            bool res = relayServerNodeStore.Node.MaxConnection == 0 || relayServerNodeStore.Node.MaxConnection * 2 > connectionNum;
            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateConnection false,{connectionNum}/{relayServerNodeStore.Node.MaxConnection * 2}");

            return res;
        }

        /// <summary>
        /// 流量是否够
        /// </summary>
        /// <returns></returns>
        public bool ValidateBytes()
        {
            bool res = relayServerNodeStore.Node.MaxGbTotal == 0
                || (relayServerNodeStore.Node.MaxGbTotal > 0 && relayServerNodeStore.Node.MaxGbTotalLastBytes > 0);

            if (res == false && LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"relay  ValidateBytes false,{relayServerNodeStore.Node.MaxGbTotalLastBytes}bytes/{relayServerNodeStore.Node.MaxGbTotal}gb");

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
            if (relayServerNodeStore.Node.MaxGbTotal == 0)
            {
                return true;
            }

            if (relayServerNodeStore.Node.MaxGbTotalLastBytes >= length)
                relayServerNodeStore.SetMaxGbTotalLastBytes(relayServerNodeStore.Node.MaxGbTotalLastBytes - length);
            else relayServerNodeStore.SetMaxGbTotalLastBytes(0);
            return relayServerNodeStore.Node.MaxGbTotalLastBytes > 0;
        }

        /// <summary>
        /// 获取单个限速
        /// </summary>
        /// <returns></returns>
        public uint GetBandwidthLimit()
        {
            return (uint)Math.Ceiling((relayServerNodeStore.Node.MaxBandwidth * 1024 * 1024) / 8.0);
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
            if (relayServerNodeStore.Node.MaxGbTotalMonth != DateTime.Now.Month)
            {
                relayServerNodeStore.SetMaxGbTotalMonth(DateTime.Now.Month);
                relayServerNodeStore.SetMaxGbTotalLastBytes((ulong)(relayServerNodeStore.Node.MaxGbTotal * 1024 * 1024 * 1024));
            }
            relayServerNodeStore.Confirm();
        }

        private void ReportTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                ResetBytes();
                IEnumerable<RelayServerNodeInfo> nodes = new List<RelayServerNodeInfo>
                {
                    //默认报告给自己，作为本服务器的一个默认中继节点
                    new RelayServerNodeInfo{
                        Id = RelayServerNodeInfo.MASTER_NODE_ID,
                        Host = new IPEndPoint(IPAddress.Any, relayServerNodeStore.ServicePort).ToString(),
                        MasterHost =  new IPEndPoint(IPAddress.Loopback, relayServerNodeStore.ServicePort).ToString(),
                        MasterSecretKey = relayServerMasterStore.Master.SecretKey,
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
                    relayServerNodeStore.Node
                }.Where(c => string.IsNullOrWhiteSpace(c.MasterHost) == false && string.IsNullOrWhiteSpace(c.MasterSecretKey) == false)
                .Where(c => string.IsNullOrWhiteSpace(c.Name) == false && string.IsNullOrWhiteSpace(c.Id) == false);

                double diff = (bytes - lastBytes) * 8 / 1024.0 / 1024.0;
                lastBytes = bytes;

                foreach (var node in nodes)
                {
                    try
                    {
                        ICrypto crypto = node.Id == RelayServerNodeInfo.MASTER_NODE_ID ? cryptoMaster : cryptoNode;
                        IPEndPoint endPoint = await NetworkHelper.GetEndPointAsync(node.Host, relayServerNodeStore.ServicePort) ?? new IPEndPoint(IPAddress.Any, relayServerNodeStore.ServicePort);

                        RelayServerNodeReportInfo relayNodeReportInfo = new RelayServerNodeReportInfo
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

                        IPEndPoint ep = await NetworkHelper.GetEndPointAsync(node.MasterHost, relayServerNodeStore.ServicePort);

                        byte[] content = crypto.Encode(serializer.Serialize(relayNodeReportInfo));
                        byte[] data = new byte[content.Length + 1];
                        data[0] = relayServerNodeStore.Flag;
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
