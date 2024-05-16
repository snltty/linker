using cmonitor.client;
using cmonitor.client.running;
using cmonitor.config;
using cmonitor.plugins.tuntap.messenger;
using cmonitor.plugins.tuntap.proxy;
using cmonitor.plugins.tuntap.vea;
using cmonitor.server;
using common.libs;
using MemoryPack;
using System.Buffers.Binary;
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


        private List<TuntapInfo> tuntapInfos = new List<TuntapInfo>();
        public List<TuntapInfo> Infos => tuntapInfos;


        private bool starting = false;
        public TuntapStatus Status => tuntapVea.Running ? TuntapStatus.Running : (starting ? TuntapStatus.Starting : TuntapStatus.Normal);


        public TuntapTransfer(MessengerSender messengerSender, ClientSignInState clientSignInState, ITuntapVea tuntapVea, Config config, TuntapProxy tuntapProxy)
        {
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.tuntapVea = tuntapVea;
            this.config = config;
            this.tuntapProxy = tuntapProxy;

            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                if (config.Data.Client.Tuntap.Running) { _ = Run(); }
                else { _ = SendChange(); }
            };
        }

        public async Task<bool> Run()
        {
            if (starting)
            {
                return false;
            }
            if (CheckIp() == false)
            {
                Logger.Instance.Error($"tuntap ip {config.Data.Client.Tuntap.IP} invalid");
                return false;
            }

            starting = true;
            try
            {
                bool result = await tuntapVea.Run(tuntapProxy.LocalEndpoint.Port);
                if (result)
                {
                    await tuntapVea.SetIp(config.Data.Client.Tuntap.IP);
                    List<TuntapInfo> infos = await GetRemoteInfo();
                    SetIPs(infos);
                }
                config.Data.Client.Tuntap.Running = Status == TuntapStatus.Running;
                config.Save();
                await SendChange();
                return result;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            finally
            {
                starting = false;
            }
            return false;
        }
        public async Task Stop()
        {
            if (starting)
            {
                return;
            }
            starting = true;
            try
            {
                tuntapVea.Kill();
                config.Data.Client.Tuntap.Running = Status == TuntapStatus.Running;
                config.Save();
                await SendChange();
            }
            catch (Exception ex)
            {
                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                {
                    Logger.Instance.Error(ex);
                }
            }
            starting = false;
        }

        public void OnChange()
        {
            GetRemoteInfo().ContinueWith((result) =>
            {
                SetIPs(result.Result);
            });
        }
        public TuntapInfo GetInfo()
        {
            return new TuntapInfo { IP = config.Data.Client.Tuntap.IP, LanIPs = config.Data.Client.Tuntap.LanIPs, MachineName = config.Data.Client.Name, Status = Status };
        }

        public void Update(TuntapInfo info)
        {
            config.Data.Client.Tuntap.IP = info.IP;
            config.Data.Client.Tuntap.LanIPs = info.LanIPs;
            config.Save();
            if (Status == TuntapStatus.Running)
            {
                try
                {
                    tuntapVea.Kill();
                    _ = Run();
                }
                catch (Exception)
                {
                }
            }
            else
            {
                _ = SendChange();
            }
        }

        private async Task SendChange()
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.ChangeForward
            });
            OnChange();
        }
        private void SetIPs(List<TuntapInfo> infos)
        {
            List<TuntapVeaLanIPAddressList> ipsList = ParseIPs(tuntapInfos);
            TuntapVeaLanIPAddress[] ips = ipsList.SelectMany(c => c.IPS).ToArray();
            tuntapVea.DelRoute(ips);

            tuntapInfos = infos;

            ipsList = ParseIPs(tuntapInfos);
            ips = ipsList.SelectMany(c => c.IPS).ToArray();
            tuntapVea.AddRoute(ips, config.Data.Client.Tuntap.IP);

            tuntapProxy.SetIPs(ipsList);
        }
        private async Task<List<TuntapInfo>> GetRemoteInfo()
        {
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = clientSignInState.Connection,
                MessengerId = (ushort)TuntapMessengerIds.InfoForward
            });
            if (resp.Code != MessageResponeCodes.OK)
            {
                return null;
            }

            List<TuntapInfo> infos = MemoryPackSerializer.Deserialize<List<TuntapInfo>>(resp.Data.Span);
            infos.Add(GetInfo());
            return infos;
        }

        private bool CheckIp()
        {
            uint maskValue = NetworkHelper.MaskValue(24);
            uint[] networks = NetworkHelper.GetIPV4()
                .Select(c => BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes()) & maskValue)
                .ToArray();

            uint network = BinaryPrimitives.ReadUInt32BigEndian(config.Data.Client.Tuntap.IP.GetAddressBytes()) & maskValue;
            return config.Data.Client.Tuntap.IP.Equals(IPAddress.Any) == false && networks.Contains(network) == false;
        }

        private List<TuntapVeaLanIPAddressList> ParseIPs(List<TuntapInfo> infos)
        {
            uint maskValue = NetworkHelper.MaskValue(24);
            uint[] networks = NetworkHelper.GetIPV4().Concat(new IPAddress[] { config.Data.Client.Tuntap.IP })
                .Select(c => BinaryPrimitives.ReadUInt32BigEndian(c.GetAddressBytes()) & maskValue)
                .ToArray();

            return infos
                //自己的ip不要
                .Where(c => c.IP.Equals(config.Data.Client.Tuntap.IP) == false)
                .Select(c =>
                {
                    return new TuntapVeaLanIPAddressList
                    {
                        MachineName = c.MachineName,
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
                return ParseIPAddress(c);

            }).ToList();
        }
        private TuntapVeaLanIPAddress ParseIPAddress(IPAddress ip)
        {
            uint ipInt = BinaryPrimitives.ReadUInt32BigEndian(ip.GetAddressBytes());
            byte maskLength = 24;
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
