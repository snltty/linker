using cmonitor.client;
using cmonitor.client.config;
using cmonitor.config;
using cmonitor.plugins.tuntap.messenger;
using cmonitor.plugins.tuntap.proxy;
using cmonitor.plugins.tuntap.vea;
using cmonitor.server;
using common.libs;
using MemoryPack;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;

namespace cmonitor.plugins.tuntap
{
    public sealed class TuntapTransfer
    {
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly ITuntapVea tuntapVea;
        private readonly Config config;
        private readonly TuntapProxy tuntapProxy;
        private readonly RunningConfig runningConfig;

        private uint infosVersion = 0;
        private readonly ConcurrentDictionary<string, TuntapInfo> tuntapInfos = new ConcurrentDictionary<string, TuntapInfo>();
        public ConcurrentDictionary<string, TuntapInfo> Infos => tuntapInfos;
        public uint InfosVersion => infosVersion;


        private bool starting = false;
        public TuntapStatus Status => tuntapVea.Running ? TuntapStatus.Running : (starting ? TuntapStatus.Starting : TuntapStatus.Normal);

        public TuntapTransfer(MessengerSender messengerSender, ClientSignInState clientSignInState, ITuntapVea tuntapVea, Config config, TuntapProxy tuntapProxy, RunningConfig runningConfig)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.tuntapVea = tuntapVea;
            this.config = config;
            this.tuntapProxy = tuntapProxy;
            this.runningConfig = runningConfig;

            tuntapVea.Kill();
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                if (runningConfig.Data.Tuntap.Running) { Run(); }
            };
            clientSignInState.NetworkEnabledHandle += (times) =>
            {
                OnChange();
            };

            AppDomain.CurrentDomain.ProcessExit += (s, e) => StopExit();
            Console.CancelKeyPress += (s, e) => StopExit();
        }

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
                    bool result = await tuntapVea.Run(tuntapProxy.LocalEndpoint.Port, runningConfig.Data.Tuntap.IP);
                    runningConfig.Data.Tuntap.Running = Status == TuntapStatus.Running;
                    runningConfig.Data.Update();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
                finally
                {
                    BooleanHelper.CompareExchange(ref starting, false, true);
                    OnChange();
                }
            });
        }
        public void Stop()
        {
            if (BooleanHelper.CompareExchange(ref starting, true, false))
            {
                return;
            }
            try
            {
                OnChange();
                tuntapVea.Kill();
                runningConfig.Data.Tuntap.Running = Status == TuntapStatus.Running;
                runningConfig.Data.Update();
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            finally
            {
                BooleanHelper.CompareExchange(ref starting, false, true);
                OnChange();
            }
        }

        private void StopExit()
        {
            bool running = runningConfig.Data.Tuntap.Running;
            Stop();
            runningConfig.Data.Tuntap.Running = running;
            runningConfig.Data.Update();
        }


        public void RefreshInfo()
        {
            OnChange();
        }
        /// <summary>
        /// 更新本机信息
        /// </summary>
        /// <param name="info"></param>
        public void OnUpdate(TuntapInfo info)
        {
            Task.Run(() =>
            {
                runningConfig.Data.Tuntap.IP = info.IP;
                runningConfig.Data.Tuntap.LanIPs = info.LanIPs;
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
        /// 更新远程主机信息
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

        private TuntapInfo GetLocalInfo()
        {
            return new TuntapInfo { IP = runningConfig.Data.Tuntap.IP, LanIPs = runningConfig.Data.Tuntap.LanIPs, MachineId = config.Data.Client.Id, Status = Status };
        }
        private async Task<List<TuntapInfo>> GetRemoteInfo()
        {
            TuntapInfo info = GetLocalInfo();
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.ConfigForward,
                Payload = MemoryPackSerializer.Serialize(info),
                Timeout = 3000
            });
            if (resp.Code != MessageResponeCodes.OK)
            {
                return null;
            }

            List<TuntapInfo> infos = MemoryPackSerializer.Deserialize<List<TuntapInfo>>(resp.Data.Span);
            infos.Add(info);
            return infos;
        }

        private void DelRoute()
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapInfos.Values.ToList());
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            tuntapVea.DelRoute(ips);
        }
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
        private bool CheckIp()
        {
            uint maskValue = NetworkHelper.MaskValue(24);
            uint[] networks = NetworkHelper.GetIPV4()
                .Select(c => BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes()) & maskValue)
                .ToArray();

            uint network = BinaryPrimitives.ReadUInt32BigEndian(runningConfig.Data.Tuntap.IP.GetAddressBytes()) & maskValue;
            return runningConfig.Data.Tuntap.IP.Equals(IPAddress.Any) == false && networks.Contains(network) == false;
        }

        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            uint maskValue = NetworkHelper.MaskValue(24);
            uint[] networks = NetworkHelper.GetIPV4().Concat(new IPAddress[] { runningConfig.Data.Tuntap.IP })
                .Select(c => BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes()) & maskValue)
                .ToArray();

            return infos
                //自己的ip不要
                .Where(c => c.IP.Equals(runningConfig.Data.Tuntap.IP) == false)
                .Select(c =>
                {
                    return new TuntapVeaLanIPAddressList
                    {
                        MachineId = c.MachineId,
                        IPS = ParseIPs(c.LanIPs)
                    };
                })
                //这边的局域网IP也不要，为了防止将本机局域网IP路由到别的地方
                .Where(c =>
                {
                    for (int i = 0; i < c.IPS.Count; i++)
                    {
                        for (int j = 0; j < networks.Length; j++)
                        {
                            if (c.IPS[i].NetWork == networks[j])
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }).ToList();
        }
        private List<TuntapVeaLanIPAddress> ParseIPs(IPAddress[] lanIPs)
        {
            return lanIPs.Where(c => c.Equals(IPAddress.Any) == false).Select(c =>
            {
                return ParseIPAddress(c, 24);

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
    }
}
