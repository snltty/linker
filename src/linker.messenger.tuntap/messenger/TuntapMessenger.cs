using linker.libs;
using linker.messenger.signin;
using linker.messenger.tuntap.cidr;
using linker.messenger.tuntap.lease;

namespace linker.messenger.tuntap.messenger
{
    public sealed class TuntapClientMessenger : IMessenger
    {
        private readonly TuntapTransfer tuntapTransfer;
        private readonly TuntapConfigTransfer tuntapConfigTransfer;
        private readonly TuntapCidrDecenterManager tuntapCidrDecenterManager;
        private readonly LeaseClientTreansfer leaseClientTreansfer;
        private readonly TuntapPingTransfer pingTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly TuntapAdapter tuntapAdapter;
        private readonly ISerializer serializer;
        public TuntapClientMessenger(TuntapTransfer tuntapTransfer, TuntapConfigTransfer tuntapConfigTransfer,
            TuntapCidrDecenterManager tuntapCidrDecenterManager, LeaseClientTreansfer leaseClientTreansfer, TuntapPingTransfer pingTransfer, IMessengerSender messengerSender, TuntapAdapter tuntapAdapter, ISerializer serializer)
        {
            this.tuntapTransfer = tuntapTransfer;
            this.tuntapConfigTransfer = tuntapConfigTransfer;
            this.tuntapCidrDecenterManager = tuntapCidrDecenterManager;
            this.leaseClientTreansfer = leaseClientTreansfer;
            this.pingTransfer = pingTransfer;
            this.messengerSender = messengerSender;
            this.tuntapAdapter = tuntapAdapter;
            this.serializer = serializer;
        }

        /// <summary>
        /// 运行网卡
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Run)]
        public void Run(IConnection connection)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"messenger restarting device");
            _ = tuntapAdapter.RetstartDevice();
        }

        /// <summary>
        /// 停止网卡
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Stop)]
        public void Stop(IConnection connection)
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Warning($"messenger stop device");
            tuntapAdapter.StopDevice();
        }

        /// <summary>
        /// 更新网卡信息
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            TuntapInfo info = serializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            tuntapConfigTransfer.Update(info);
        }
        /// <summary>
        /// 重新租赁
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.LeaseChange)]
        public void LeaseChange(IConnection connection)
        {
            tuntapConfigTransfer.RefreshIP();
        }


        [MessengerId((ushort)TuntapMessengerIds.SubscribeForwardTest)]
        public void SubscribeForwardTest(IConnection connection)
        {
            TuntapForwardTestWrapInfo tuntapForwardTestWrapInfo = serializer.Deserialize<TuntapForwardTestWrapInfo>(connection.ReceiveRequestWrap.Payload.Span);

            uint requestid = connection.ReceiveRequestWrap.RequestId;
            pingTransfer.SubscribeForwardTest(tuntapForwardTestWrapInfo.List).ContinueWith((result) =>
            {
                messengerSender.ReplyOnly(new MessageResponseWrap
                {
                    Connection = connection,
                    RequestId = requestid,
                    Code = MessageResponeCodes.OK,
                    Payload = serializer.Serialize(tuntapForwardTestWrapInfo)
                }, (ushort)TuntapMessengerIds.SubscribeForwardTest);
            });
        }


        [MessengerId((ushort)TuntapMessengerIds.Routes)]
        public void Routes(IConnection connection)
        {
            connection.Write(serializer.Serialize(tuntapCidrDecenterManager.CidrRoutes));
        }
        [MessengerId((ushort)TuntapMessengerIds.ID)]
        public void ID(IConnection connection)
        {
            connection.Write(serializer.Serialize(tuntapConfigTransfer.Info.Guid));
        }
        [MessengerId((ushort)TuntapMessengerIds.SetID)]
        public void SetID(IConnection connection)
        {
            tuntapConfigTransfer.SetID(serializer.Deserialize<Guid>(connection.ReceiveRequestWrap.Payload.Span));
        }
    }


    public sealed class TuntapServerMessenger : IMessenger
    {
        private readonly IMessengerSender messengerSender;
        private readonly SignInServerCaching signCaching;
        private readonly LeaseServerTreansfer leaseTreansfer;
        private readonly ISerializer serializer;
        public TuntapServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, LeaseServerTreansfer leaseTreansfer, ISerializer serializer)
        {
            this.messengerSender = messengerSender;
            this.signCaching = signCaching;
            this.leaseTreansfer = leaseTreansfer;
            this.serializer = serializer;
        }

        /// <summary>
        /// 转发运行命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.RunForward)]
        public async Task RunForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Run
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 转发停止命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.StopForward)]
        public async Task StopForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Stop
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 转发更新网卡信息命令
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.UpdateForward)]
        public async Task UpdateForward(IConnection connection)
        {
            TuntapInfo info = serializer.Deserialize<TuntapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    Timeout = 3000,
                    MessengerId = (ushort)TuntapMessengerIds.Update,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 添加网络配置
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.LeaseAddNetwork)]
        public void LeaseAddNetwork(IConnection connection)
        {
            LeaseInfo info = serializer.Deserialize<LeaseInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                leaseTreansfer.AddNetwork(cache.GroupId, info);
            }
        }
        /// <summary>
        /// 获取网络配置
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.LeaseGetNetwork)]
        public void LeaseGetNetwork(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                connection.Write(serializer.Serialize(leaseTreansfer.GetNetwork(cache.GroupId)));
            }
        }
        /// <summary>
        /// 租赁IP
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)TuntapMessengerIds.LeaseIP)]
        public void LeaseIP(IConnection connection)
        {
            LeaseInfo info = serializer.Deserialize<LeaseInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                LeaseInfo result = leaseTreansfer.LeaseIP(cache.MachineId, cache.GroupId, info);
                connection.Write(serializer.Serialize(result));
            }
        }
        /// <summary>
        /// 网络配置发生变化，需要重新租赁
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.LeaseChangeForward)]
        public void LeaseChangeForward(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                uint requiestid = connection.ReceiveRequestWrap.RequestId;

                List<SignCacheInfo> caches = signCaching.Get(cache);
                IEnumerable<Task<bool>> tasks = caches.Where(c => c.MachineId != connection.Id && c.Connected).Select(c => messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = c.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.LeaseChange,
                    Payload = connection.ReceiveRequestWrap.Payload,
                    Timeout = 1000,
                })).ToList();
                Task.WhenAll(tasks);
            }
        }


        /// <summary>
        /// 续期
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)TuntapMessengerIds.LeaseExp)]
        public void LeaseExp(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                leaseTreansfer.LeaseExp(cache.MachineId, cache.GroupId);
            }
        }



        [MessengerId((ushort)TuntapMessengerIds.SubscribeForwardTestForward)]
        public void SubscribeForwardTestForward(IConnection connection)
        {
            uint requestid = connection.ReceiveRequestWrap.RequestId;
            TuntapForwardTestWrapInfo info = serializer.Deserialize<TuntapForwardTestWrapInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {

                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.SubscribeForwardTest,
                    Payload = connection.ReceiveRequestWrap.Payload
                }).ContinueWith((result) =>
                {
                    messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        RequestId = requestid,
                        Code = MessageResponeCodes.OK,
                        Payload = result.Result.Data
                    }, (ushort)TuntapMessengerIds.SubscribeForwardTestForward);
                });
            }
            else
            {
                messengerSender.ReplyOnly(new MessageResponseWrap
                {
                    Connection = connection,
                    RequestId = requestid,
                    Code = MessageResponeCodes.OK,
                    Payload = Helper.EmptyArray
                }, (ushort)TuntapMessengerIds.SubscribeForwardTestForward);
            }
        }



        [MessengerId((ushort)TuntapMessengerIds.RoutesForward)]
        public void RoutesForward(IConnection connection)
        {
            uint requestid = connection.ReceiveRequestWrap.RequestId;
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {

                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.Routes,
                }).ContinueWith((result) =>
                {
                    messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        RequestId = requestid,
                        Code = MessageResponeCodes.OK,
                        Payload = result.Result.Data
                    }, (ushort)TuntapMessengerIds.RoutesForward);
                });
            }
            else
            {
                messengerSender.ReplyOnly(new MessageResponseWrap
                {
                    Connection = connection,
                    RequestId = requestid,
                    Code = MessageResponeCodes.OK,
                    Payload = Helper.EmptyArray
                }, (ushort)TuntapMessengerIds.RoutesForward);
            }
        }

        [MessengerId((ushort)TuntapMessengerIds.IDForward)]
        public void IDForward(IConnection connection)
        {
            uint requestid = connection.ReceiveRequestWrap.RequestId;
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.ID,
                    Timeout = 1000
                }).ContinueWith((result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK)
                    {
                        messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            RequestId = requestid,
                            Code = MessageResponeCodes.OK,
                            Payload = result.Result.Data
                        }, (ushort)TuntapMessengerIds.IDForward);
                    }
                });
            }
        }

        [MessengerId((ushort)TuntapMessengerIds.SetIDForward)]
        public async Task SetIDForward(IConnection connection)
        {
            KeyValuePair<string, Guid> info = serializer.Deserialize<KeyValuePair<string, Guid>>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.Key, out SignCacheInfo from, out SignCacheInfo to))
            {
                await messengerSender.SendOnly(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)TuntapMessengerIds.SetID,
                    Payload = serializer.Serialize(info.Value)
                }).ConfigureAwait(false);
            }
        }
    }
}
