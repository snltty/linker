﻿using linker.libs;
using linker.libs.extends;
using linker.messenger.exroute;
using linker.messenger.signin;
using linker.tun;
using linker.tunnel.connection;

namespace linker.messenger.tuntap
{
    public sealed class TuntapAdapter : ILinkerTunDeviceCallback, ITuntapProxyCallback
    {
        private List<LinkerTunDeviceForwardItem> forwardItems = new List<LinkerTunDeviceForwardItem>();

        private bool skipCheck => tuntapTransfer.Status == TuntapStatus.Operating || tuntapConfigTransfer.Running == false;
        private bool needRestart => tuntapTransfer.Status != TuntapStatus.Running || tuntapConfigTransfer.Changed;

        private readonly TuntapTransfer tuntapTransfer;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapDecenter tuntapDecenter;
        private readonly TuntapProxy tuntapProxy;
        private readonly ISignInClientStore signInClientStore;
        private readonly ExRouteTransfer exRouteTransfer;

        public TuntapAdapter(TuntapTransfer tuntapTransfer, TuntapConfigTransfer tuntapConfigTransfer, TuntapDecenter tuntapDecenter, TuntapProxy tuntapProxy,
            SignInClientState signInClientState, ISignInClientStore signInClientStore, ExRouteTransfer exRouteTransfer)
        {
            this.tuntapTransfer = tuntapTransfer;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapDecenter = tuntapDecenter;
            this.tuntapProxy = tuntapProxy;
            this.signInClientStore = signInClientStore;
            this.exRouteTransfer = exRouteTransfer;

            //与服务器连接，刷新一下IP
            signInClientState.OnSignInSuccess += (times) => tuntapConfigTransfer.RefreshIP();

            //初始化网卡
            tuntapTransfer.Init(this);
            //网卡状态发生变化，同步一下信息
            tuntapTransfer.OnSetupBefore += () =>
            {
                tuntapConfigTransfer.SetRunning(true);
                tuntapDecenter.Refresh();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning("tuntap setup before");
            };
            tuntapTransfer.OnSetupAfter += () =>
            {
                tuntapDecenter.Refresh();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning("tuntap setup after");
            };
            tuntapTransfer.OnSetupSuccess += () =>
            {
                AddForward();
            };
            tuntapTransfer.OnShutdownBefore += () =>
            {
                tuntapDecenter.Refresh();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning("tuntap shutdown before");
            };
            tuntapTransfer.OnShutdownAfter += () =>
            {
                tuntapDecenter.Refresh(); DeleteForward();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Warning("tuntap shutdown after");
                tuntapConfigTransfer.SetRunning(false);
            };

            //配置有更新，去同步一下
            tuntapConfigTransfer.OnUpdate += () => { AddForward(); _ = CheckDevice(); tuntapDecenter.Refresh(); };

            //隧道回调
            tuntapProxy.Callback = this;
            CheckDeviceTask();
        }


        private OperatingManager checking = new OperatingManager();
        private void CheckDeviceTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                await CheckDevice().ConfigureAwait(false);
                return true;
            }, 5000);
        }
        private async Task CheckDevice()
        {
            try
            {
                if (checking.StartOperation() == false || skipCheck)
                {
                    return;
                }
                if (needRestart)
                {
                    await RetstartDevice().ConfigureAwait(false);
                    return;
                }
                if (await tuntapTransfer.CheckAvailable().ConfigureAwait(false) == false)
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

        public async Task Callback(LinkerTunDevicPacket packet)
        {
            if (packet.IPV4Broadcast || packet.IPV6Multicast)
            {
                if ((tuntapConfigTransfer.Switch & TuntapSwitch.Multicast) == TuntapSwitch.Multicast)
                {
                    return;
                }
            }
            await tuntapProxy.InputPacket(packet).ConfigureAwait(false);
        }

        public async ValueTask Close(ITunnelConnection connection)
        {
            tuntapDecenter.Refresh();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
        public void Receive(ITunnelConnection connection, ReadOnlyMemory<byte> buffer)
        {
            tuntapTransfer.Write(buffer);
        }
        public async ValueTask NotFound(uint ip)
        {
            tuntapDecenter.Refresh();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// 重启网卡
        /// </summary>
        /// <returns></returns>
        public async Task RetstartDevice()
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"restart, stop device");
            tuntapTransfer.Shutdown();
            await tuntapConfigTransfer.RefreshIPASync().ConfigureAwait(false);
            tuntapTransfer.Setup(tuntapConfigTransfer.Name, tuntapConfigTransfer.Info.IP, tuntapConfigTransfer.Info.PrefixLength, tuntapConfigTransfer.Info.DisableNat == false);
        }
        /// <summary>
        /// 关闭网卡
        /// </summary>
        public void StopDevice()
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"stop device");
            tuntapTransfer.Shutdown();
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
