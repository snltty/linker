using linker.libs;
using linker.libs.extends;
using linker.libs.timer;
using linker.messenger.exroute;
using linker.messenger.signin;
using linker.nat;
using linker.tun.device;
using linker.tunnel.connection;
using System.Net;
using static linker.nat.LinkerDstMapping;

namespace linker.messenger.tuntap
{
    public sealed class TuntapAdapter : ILinkerTunDeviceCallback, ITuntapProxyCallback
    {
        private List<LinkerTunDeviceForwardItem> forwardItems = [];

        private bool SkipCheck => tuntapTransfer.Status == TuntapStatus.Operating || tuntapConfigTransfer.Info.Running == false;
        private bool NeedRestart => tuntapTransfer.Status != TuntapStatus.Running || tuntapConfigTransfer.Changed;

        private readonly TuntapTransfer tuntapTransfer;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapDecenter tuntapDecenter;
        private readonly TuntapProxy tuntapProxy;

        public TuntapAdapter(TuntapTransfer tuntapTransfer, TuntapConfigTransfer tuntapConfigTransfer, TuntapDecenter tuntapDecenter, TuntapProxy tuntapProxy, SignInClientState signInClientState, ISignInClientStore signInClientStore, ExRouteTransfer exRouteTransfer)
        {
            this.tuntapTransfer = tuntapTransfer;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapDecenter = tuntapDecenter;
            this.tuntapProxy = tuntapProxy;

            //初始化网卡
            tuntapTransfer.Initialize(this);

            //与服务器连接，刷新一下IP
            signInClientState.OnSignInSuccess += SignInSuccess;

            //网卡状态发生变化，同步一下信息
            tuntapTransfer.OnSetupBefore += SetupBefore;
            tuntapTransfer.OnSetupAfter += SetupAfter;
            tuntapTransfer.OnSetupSuccess += SetupSuccess;
            tuntapTransfer.OnShutdownBefore += ShutdownBefore;
            tuntapTransfer.OnShutdownAfter += ShutdownAfter;
            //配置有更新，去同步一下
            tuntapConfigTransfer.OnUpdate += Update;

            //隧道回调
            tuntapProxy.Callback = this;
            TimerHelper.SetIntervalLong(CheckDevice, 30000);
        }

        private void SignInSuccess(int times)
        {
            _ = CheckDevice();
            FireWallHelper.WriteIcmp(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, tuntapConfigTransfer.Info.IP, tuntapConfigTransfer.Info.PrefixLength);
        }
        private void Update()
        {
            SetNat();
            SetMaps();
            AddForward();
            tuntapDecenter.Refresh();
            _ = CheckDevice();
        }

        private void SetupBefore()
        {
            tuntapConfigTransfer.SetRunning(true);
            tuntapDecenter.Refresh();
        }
        private void SetupAfter()
        {
            tuntapDecenter.Refresh();
        }
        private void SetupSuccess()
        {
            SetNat();
            SetMaps();
            AddForward();
        }
        private void ShutdownBefore()
        {
            tuntapDecenter.Refresh();
        }
        private void ShutdownAfter()
        {
            RemoveNat();
            RemoveMaps();
            DeleteForward();
            tuntapDecenter.Refresh();
        }


        public async Task Callback(LinkerTunDevicPacket packet)
        {
            await tuntapProxy.InputPacket(packet).ConfigureAwait(false);
        }
        public async Task Callback(LinkerSrcProxyReadPacket packet)
        {
            await tuntapProxy.InputPacket(packet).ConfigureAwait(false);
        }
        public bool Callback(uint ip)
        {
            return tuntapProxy.TestIp(ip);
        }

        public async ValueTask Close(ITunnelConnection connection)
        {
            //tuntapDecenter.Refresh();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
        public async ValueTask Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer)
        {
            await tuntapTransfer.Write(connection.RemoteMachineId, buffer).ConfigureAwait(false);
        }


        private readonly OperatingManager checking = new OperatingManager();
        /// <summary>
        /// 检查网卡设备
        /// </summary>
        /// <returns></returns>
        private async Task CheckDevice()
        {
            try
            {
                if (checking.StartOperation() == false || SkipCheck)
                {
                    return;
                }
                if (NeedRestart)
                {
                    await RetstartDevice().ConfigureAwait(false);
                    return;
                }
                if (await tuntapTransfer.CheckAvailable(tuntapConfigTransfer.Info.InterfaceOrder).ConfigureAwait(false) == false)
                {
                    tuntapTransfer.Refresh();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                checking.StopOperation();
            }
        }
        /// <summary>
        /// 重启网卡
        /// </summary>
        /// <returns></returns>
        public async Task RetstartDevice()
        {
            tuntapTransfer.Shutdown();
            await tuntapConfigTransfer.RefreshIPAsync().ConfigureAwait(false);
            tuntapTransfer.Setup(new LinkerTunDeviceSetupInfo
            {
                Name = tuntapConfigTransfer.Name,
                Address = tuntapConfigTransfer.Info.IP,
                PrefixLength = tuntapConfigTransfer.Info.PrefixLength,
                Mtu = 1420,
                Guid = tuntapConfigTransfer.Info.Guid,
            });
        }
        /// <summary>
        /// 关闭网卡
        /// </summary>
        public void StopDevice()
        {
            if (tuntapTransfer.Shutdown())
            {
                tuntapConfigTransfer.SetRunning(false);
            }
        }

        /// <summary>
        /// 设置映射
        /// </summary>
        private void SetMaps()
        {
            var maps = tuntapConfigTransfer.Info.Lans
                .Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false && c.MapIP != null && c.MapIP.Equals(IPAddress.Any) == false && c.Disabled == false)
                .Select(c => new DstMapInfo { FakeIP = c.IP, RealIP = c.MapIP, PrefixLength = c.MapPrefixLength }).ToArray();
            tuntapTransfer.SetDstMap(maps);
        }
        /// <summary>
        /// 移除映射
        /// </summary>
        private void RemoveMaps()
        {
            tuntapTransfer.RemoveDstMap();
        }

        /// <summary>
        /// 设置NAT
        /// </summary>
        private void SetNat()
        {
            if (tuntapConfigTransfer.Info.DisableNat == false)
            {
                List<TuntapLanInfo> lans = tuntapConfigTransfer.Info.Lans.Where(c => c.IP != null && c.IP.Equals(IPAddress.Any) == false && c.MapIP != null && c.Disabled == false).ToList();

                if (lans.Count != 0)
                {
                    tuntapTransfer.SetNat(lans.Select(c => new LinkerTunAppNatItemInfo
                    {
                        IP = c.MapIP.Equals(IPAddress.Any) ? c.IP : c.MapIP,
                        PrefixLength = c.MapIP.Equals(IPAddress.Any) ? c.PrefixLength : c.MapPrefixLength,
                    }).ToArray());
                    return;
                }
            }
            RemoveNat();
        }
        /// <summary>
        /// 移除NAT
        /// </summary>
        private void RemoveNat()
        {
            tuntapTransfer.RemoveNat();
        }

        // <summary>
        /// 添加端口转发
        /// </summary>
        private void AddForward()
        {
            var temp = ParseForwardItems();
            var removes = forwardItems.Except(temp, new LinkerTunDeviceForwardItemComparer());
            if (removes.Any())
            {
                tuntapTransfer.RemoveForward(removes.ToList());
            }
            forwardItems = temp;
            if (tuntapTransfer.Status != TuntapStatus.Normal)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Debug($"add tuntap forward {forwardItems.ToJson()}");
                tuntapTransfer.AddForward(forwardItems);
            }
        }
        /// <summary>
        /// 删除端口转发
        /// </summary>
        public void DeleteForward()
        {
            tuntapTransfer.RemoveForward(forwardItems);
        }
        private List<LinkerTunDeviceForwardItem> ParseForwardItems()
        {
            return tuntapConfigTransfer.Info.Forwards.Select(c => new LinkerTunDeviceForwardItem { ListenAddr = c.ListenAddr, ListenPort = c.ListenPort, ConnectAddr = c.ConnectAddr, ConnectPort = c.ConnectPort }).ToList();
        }


    }
}
