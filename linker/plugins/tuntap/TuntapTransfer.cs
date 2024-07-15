using linker.client;
using linker.client.config;
using linker.config;
using linker.plugins.tuntap.messenger;
using linker.plugins.tuntap.proxy;
using linker.plugins.tuntap.vea;
using linker.server;
using linker.libs;
using MemoryPack;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using linker.libs.extends;

namespace linker.plugins.tuntap
{
    public sealed class TuntapTransfer
    {
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly ITuntapVea tuntapVea;
        private readonly FileConfig config;
        private readonly TuntapProxy tuntapProxy;
        private readonly RunningConfig runningConfig;

        private uint infosVersion = 0;
        private readonly ConcurrentDictionary<string, TuntapInfo> tuntapInfos = new ConcurrentDictionary<string, TuntapInfo>();
        public ConcurrentDictionary<string, TuntapInfo> Infos => tuntapInfos;
        public uint InfosVersion => infosVersion;


        private bool starting = false;
        public TuntapStatus Status => tuntapVea.Running ? TuntapStatus.Running : (starting ? TuntapStatus.Starting : TuntapStatus.Normal);

        public TuntapTransfer(MessengerSender messengerSender, ClientSignInState clientSignInState, ITuntapVea tuntapVea, FileConfig config, TuntapProxy tuntapProxy, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.tuntapVea = tuntapVea;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
            this.runningConfig = runningConfig;

            tuntapVea.Kill();
            clientSignInState.NetworkEnabledHandle += (times) =>
            {
                OnChange();
            };
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                OnChange();
                if (runningConfig.Data.Tuntap.Running)
                {
                    Stop(); Run();
                    _ = CheckVeaStatusTask();
                }
            };

            AppDomain.CurrentDomain.ProcessExit += (s, e) => OnExit();
            Console.CancelKeyPress += (s, e) => OnExit();
        }

        /// <summary>
        /// 程序关闭
        /// </summary>
        private void OnExit()
        {
            bool running = runningConfig.Data.Tuntap.Running;
            Stop();
            runningConfig.Data.Tuntap.Running = running;
            runningConfig.Data.Update();
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        public void Run()
        {
            if (BooleanHelper.CompareExchange(ref starting, true, false))
            {
                return;
            }

            Task.Run(async () =>
            {
                OnChange();
                try
                {
                    if (runningConfig.Data.Tuntap.IP.Equals(IPAddress.Any))
                    {
                        return;
                    }

                    tuntapProxy.Start();
                    while (tuntapProxy.LocalEndpoint == null)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                    }

                    bool result = await tuntapVea.Run(tuntapProxy.LocalEndpoint.Port, runningConfig.Data.Tuntap.IP).ConfigureAwait(false);
                    runningConfig.Data.Tuntap.Running = Status == TuntapStatus.Running;
                    runningConfig.Data.Update();
                    if (result == false)
                    {
                        Stop();
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Instance.Error(ex);
                }
                finally
                {
                    BooleanHelper.CompareExchange(ref starting, false, true);
                    OnChange();
                }
            });
        }
        /// <summary>
        /// 停止网卡
        /// </summary>
        public void Stop()
        {
            if (BooleanHelper.CompareExchange(ref starting, true, false))
            {
                return;
            }
            try
            {
                tuntapProxy.Stop();
                OnChange();
                tuntapVea.Kill();
                runningConfig.Data.Tuntap.Running = Status == TuntapStatus.Running;
                runningConfig.Data.Update();
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    LoggerHelper.Instance.Error(ex);
                }
            }
            finally
            {
                BooleanHelper.CompareExchange(ref starting, false, true);
                OnChange();
            }
        }

        /// <summary>
        /// 刷新信息，把自己的网卡配置发给别人，顺便把别人的网卡信息带回来
        /// </summary>
        public void Refresh()
        {
            OnChange();
        }

        /// <summary>
        /// 更新本机网卡信息
        /// </summary>
        /// <param name="info"></param>
        public void OnUpdate(TuntapInfo info)
        {
            Task.Run(() =>
            {
                runningConfig.Data.Tuntap.IP = info.IP;
                runningConfig.Data.Tuntap.LanIPs = info.LanIPs;
                runningConfig.Data.Tuntap.Masks = info.Masks;
                runningConfig.Data.Tuntap.BufferSize = info.BufferSize;
                runningConfig.Data.Update();
                if (Status == TuntapStatus.Running)
                {
                    Stop();
                    Run();
                }
                else
                {
                    OnChange();
                }
            });
        }
        /// <summary>
        /// 收到别的客户端的网卡信息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public TuntapInfo OnConfig(TuntapInfo info)
        {
            Task.Run(() =>
            {
                DelRoute();
                tuntapInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
                Interlocked.Increment(ref infosVersion);
                AddRoute();
            });

            return GetLocalInfo();
        }

        /// <summary>
        /// 信息有变化，刷新信息，把自己的网卡配置发给别人，顺便把别人的网卡信息带回来
        /// </summary>
        private void OnChange()
        {
            GetRemoteInfo().ContinueWith((result) =>
            {
                if (result.Result == null)
                {
                    OnChange();
                }
                else
                {
                    DelRoute();
                    foreach (var item in result.Result)
                    {
                        tuntapInfos.AddOrUpdate(item.MachineId, item, (a, b) => item);
                    }
                    Interlocked.Increment(ref infosVersion);
                    AddRoute();
                }
            });
        }
        /// <summary>
        /// 获取自己的网卡信息
        /// </summary>
        /// <returns></returns>
        private TuntapInfo GetLocalInfo()
        {
            TuntapInfo info = new TuntapInfo
            {
                IP = runningConfig.Data.Tuntap.IP,
                LanIPs = runningConfig.Data.Tuntap.LanIPs,
                Masks = runningConfig.Data.Tuntap.Masks,
                MachineId = config.Data.Client.Id,
                Status = Status,
                Error = tuntapVea.Error,
                BufferSize = runningConfig.Data.Tuntap.BufferSize
            };
            if (runningConfig.Data.Tuntap.Masks.Length != runningConfig.Data.Tuntap.LanIPs.Length)
            {
                runningConfig.Data.Tuntap.Masks = runningConfig.Data.Tuntap.LanIPs.Select(c => 24).ToArray();
            }

            return info;
        }
        /// <summary>
        /// 获取别人的网卡信息
        /// </summary>
        /// <returns></returns>
        private async Task<List<TuntapInfo>> GetRemoteInfo()
        {
            TuntapInfo info = GetLocalInfo();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.ConfigForward,
                Payload = MemoryPackSerializer.Serialize(info),
                Timeout = 3000
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return null;
            }

            List<TuntapInfo> infos = MemoryPackSerializer.Deserialize<List<TuntapInfo>>(resp.Data.Span);
            infos.Add(info);
            return infos;
        }

        /// <summary>
        /// 删除路由
        /// </summary>
        private void DelRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapInfos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            tuntapVea.DelRoute(ips);
        }
        /// <summary>
        /// 添加路由
        /// </summary>
        private void AddRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapInfos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            tuntapVea.AddRoute(ips, runningConfig.Data.Tuntap.IP);

            tuntapProxy.SetIPs(ipsList);
            foreach (var item in tuntapInfos.Values)
            {
                tuntapProxy.SetIP(item.MachineId, BinaryPrimitives.ReadUInt32BigEndian(item.IP.GetAddressBytes()));
            }

        }

        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            uint[] localIps = NetworkHelper.GetIPV4().Concat(new IPAddress[] { runningConfig.Data.Tuntap.IP })
                .Select(c => BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes()))
                .ToArray();

            return infos
                //自己的ip不要
                .Where(c => c.IP.Equals(runningConfig.Data.Tuntap.IP) == false)
                .Select(c =>
                {
                    return new TuntapVeaLanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(c.LanIPs, c.Masks)
                        //这边的局域网IP也不要，为了防止将本机局域网IP路由到别的地方
                        .Where(c => localIps.Select(d => d & c.MaskValue).Contains(c.NetWork) == false).ToList(),
                    };
                }).ToList();
        }
        private List<TuntapVeaLanIPAddress> ParseIPs(IPAddress[] lanIPs, int[] masks)
        {
            if (masks.Length != lanIPs.Length) masks = lanIPs.Select(c => 24).ToArray();
            return lanIPs.Where(c => c.Equals(IPAddress.Any) == false).Select((c, index) =>
            {
                return ParseIPAddress(c, (byte)masks[index]);

            }).ToList();
        }
        private TuntapVeaLanIPAddress ParseIPAddress(IPAddress ip, byte maskLength = 24)
        {
            uint ipInt = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            //掩码十进制
            uint maskValue = NetworkHelper.MaskValue(maskLength);
            return new TuntapVeaLanIPAddress
            {
                IPAddress = ipInt,
                MaskLength = maskLength,
                MaskValue = maskValue,
                NetWork = ipInt & maskValue,
                Broadcast = ipInt | (~maskValue),
            };
        }

        private async Task CheckVeaStatusTask()
        {
            while (true)
            {
                try
                {
                    if (tuntapVea.Running)
                    {
                        await CheckProxy().ConfigureAwait(false);
                        await Task.Delay(5000).ConfigureAwait(false);
                        await CheckInterface().ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                }

                await Task.Delay(15000).ConfigureAwait(false);
            }
        }
        private async Task CheckInterface()
        {
            NetworkInterface networkInterface = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(c => c.Name == tuntapVea.InterfaceName);
            if (networkInterface != null && networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                LoggerHelper.Instance.Error($"tuntap inerface {tuntapVea.InterfaceName} is {networkInterface.OperationalStatus}, restarting");
                Stop();
                await Task.Delay(5000).ConfigureAwait(false);
                Run();
            }
        }
        private async Task CheckProxy()
        {
            if (tuntapProxy.LocalEndpoint == null || tuntapProxy.LocalEndpoint.Port == 0) return;
            try
            {
                var socket = new Socket(tuntapProxy.LocalEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, tuntapProxy.LocalEndpoint.Port)).WaitAsync(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                socket.SafeClose();
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error($"tuntap proxy {ex.Message}, restarting");
                Stop();
                await Task.Delay(5000).ConfigureAwait(false);
                Run();
            }
        }
    }
}
