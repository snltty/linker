using linker.libs;
using linker.messenger.signin;

namespace linker.messenger.flow.messenger
{
    public sealed class FlowMessenger : IMessenger
    {
        private readonly FlowTransfer flowTransfer;
        private readonly flow.FlowMessenger messengerFlow;
        private readonly FlowSForward sForwardFlow;
        private readonly FlowRelay relayFlow;
        private readonly SignInServerCaching signCaching;
        private readonly ISerializer serializer;
        private readonly FlowResolver flowResolver;
        private readonly IMessengerSender messengerSender;

        private readonly DateTime start = DateTime.Now;

        public FlowMessenger(FlowTransfer flowTransfer, flow.FlowMessenger messengerFlow, FlowSForward sForwardFlow, FlowRelay relayFlow, SignInServerCaching signCaching, ISerializer serializer, FlowResolver flowResolver, IMessengerSender messengerSender)
        {
            this.flowTransfer = flowTransfer;
            this.messengerFlow = messengerFlow;
            this.sForwardFlow = sForwardFlow;
            this.relayFlow = relayFlow;
            this.signCaching = signCaching;
            this.serializer = serializer;
            this.flowResolver = flowResolver;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)FlowMessengerIds.List)]
        public void List(IConnection connection)
        {
            Dictionary<string, FlowItemInfo> dic = flowTransfer.GetFlows();

            signCaching.GetOnline(out int all, out int online);
            dic.TryAdd("_", new FlowItemInfo { FlowName = "_", ReceiveBytes = all, SendtBytes = online });

            FlowInfo serverFlowInfo = new FlowInfo
            {
                Items = dic,
                Start = start,
                Now = DateTime.Now,
            };
            connection.Write(serializer.Serialize(serverFlowInfo));
        }

        [MessengerId((ushort)FlowMessengerIds.Citys)]
        public void Citys(IConnection connection)
        {
            connection.Write(serializer.Serialize(flowResolver.GetCitys()));
        }

        [MessengerId((ushort)FlowMessengerIds.Messenger)]
        public void Messenger(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetFlows()));
        }

        [MessengerId((ushort)FlowMessengerIds.StopwatchServer)]
        public void StopwatchServer(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetStopwatch()));
        }

        [MessengerId((ushort)FlowMessengerIds.SForward)]
        public void SForward(IConnection connection)
        {
            sForwardFlow.Update();
            SForwardFlowRequestInfo info = serializer.Deserialize<SForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new SForwardFlowResponseInfo { Count = 0, Data = [], PageSize = info.PageSize, Page = info.Page }));
                return;
            }

            if (cache.Super)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                info.GroupId = cache.GroupId;
            }

            connection.Write(serializer.Serialize(sForwardFlow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Relay)]
        public void Relay(IConnection connection)
        {
            relayFlow.Update();
            RelayFlowRequestInfo info = serializer.Deserialize<RelayFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) == false)
            {
                connection.Write(serializer.Serialize(new RelayFlowResponseInfo { Count = 0, Data = [], PageSize = info.PageSize, Page = info.Page }));
                return;
            }

            if (cache.Super)
            {
                info.GroupId = string.Empty;
            }
            else
            {
                info.GroupId = cache.GroupId;
            }

            connection.Write(serializer.Serialize(relayFlow.GetFlows(info)));
        }


        [MessengerId((ushort)FlowMessengerIds.StopwatchForward)]
        public void StopwatchForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.Stopwatch
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.StopwatchForward).ConfigureAwait(false);
                    }
                });
            }
        }

        [MessengerId((ushort)FlowMessengerIds.ListForward)]
        public void ListForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.List
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.ListForward).ConfigureAwait(false);
                    }
                });
            }
        }

        [MessengerId((ushort)FlowMessengerIds.SForwardForward)]
        public void SForwardForward(IConnection connection)
        {
            SForwardFlowRequestInfo info = serializer.Deserialize<SForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.SForward,
                    Payload = connection.ReceiveRequestWrap.Payload,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.SForwardForward).ConfigureAwait(false);
                    }
                });
            }
        }

        [MessengerId((ushort)FlowMessengerIds.ForwardForward)]
        public void ForwardForward(IConnection connection)
        {
            ForwardFlowRequestInfo info = serializer.Deserialize<ForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.Forward,
                    Payload = connection.ReceiveRequestWrap.Payload,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.ForwardForward).ConfigureAwait(false);
                    }
                });
            }
        }

        [MessengerId((ushort)FlowMessengerIds.MessengerForward)]
        public void MessengerForward(IConnection connection)
        {
            string machineid = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, machineid, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.Messenger,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.MessengerForward).ConfigureAwait(false);
                    }
                });
            }
        }

        [MessengerId((ushort)FlowMessengerIds.Socks5Forward)]
        public void Socks5Forward(IConnection connection)
        {
            Socks5FlowRequestInfo info = serializer.Deserialize<Socks5FlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.Socks5,
                    Payload = connection.ReceiveRequestWrap.Payload,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.Socks5Forward).ConfigureAwait(false);
                    }
                });
            }
        }

        [MessengerId((ushort)FlowMessengerIds.TunnelForward)]
        public void TunnelForward(IConnection connection)
        {
            TunnelFlowRequestInfo info = serializer.Deserialize<TunnelFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, info.MachineId, out SignCacheInfo from, out SignCacheInfo to))
            {
                uint requestid = connection.ReceiveRequestWrap.RequestId;
                _ = messengerSender.SendReply(new MessageRequestWrap
                {
                    Connection = to.Connection,
                    MessengerId = (ushort)FlowMessengerIds.Tunnel,
                    Payload = connection.ReceiveRequestWrap.Payload,
                }).ContinueWith(async (result) =>
                {
                    if (result.Result.Code == MessageResponeCodes.OK && result.Result.Data.Length > 0)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            Payload = result.Result.Data,
                            RequestId = requestid,
                        }, (ushort)FlowMessengerIds.TunnelForward).ConfigureAwait(false);
                    }
                });
            }
        }
    }


    public sealed class FlowClientMessenger : IMessenger
    {
        private readonly flow.FlowMessenger messengerFlow;
        private readonly ISerializer serializer;
        private readonly FlowSForward sForwardFlow;
        private readonly FlowForward forwardFlow;
        private readonly FlowSocks5 socks5Flow;
        private readonly FlowTunnel tunnelFlow;
        private readonly FlowTransfer flowTransfer;

        private DateTime start = DateTime.Now;

        public FlowClientMessenger(flow.FlowMessenger messengerFlow, ISerializer serializer, FlowSForward sForwardFlow, FlowForward forwardFlow, FlowSocks5 socks5Flow, FlowTunnel tunnelFlow, FlowTransfer flowTransfer)
        {
            this.messengerFlow = messengerFlow;
            this.serializer = serializer;
            this.sForwardFlow = sForwardFlow;
            this.forwardFlow = forwardFlow;
            this.socks5Flow = socks5Flow;
            this.tunnelFlow = tunnelFlow;
            this.flowTransfer = flowTransfer;
        }

        [MessengerId((ushort)FlowMessengerIds.Stopwatch)]
        public void Stopwatch(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetStopwatch()));
        }

        [MessengerId((ushort)FlowMessengerIds.List)]
        public void List(IConnection connection)
        {
            Dictionary<string, FlowItemInfo> dic = flowTransfer.GetFlows();
            FlowInfo serverFlowInfo = new FlowInfo
            {
                Items = dic,
                Start = start,
                Now = DateTime.Now,
            };
            connection.Write(serializer.Serialize(serverFlowInfo));
        }

        [MessengerId((ushort)FlowMessengerIds.SForward)]
        public void SForward(IConnection connection)
        {
            sForwardFlow.Update();
            SForwardFlowRequestInfo info = serializer.Deserialize<SForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(sForwardFlow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Forward)]
        public void Forward(IConnection connection)
        {
            forwardFlow.Update();
            ForwardFlowRequestInfo info = serializer.Deserialize<ForwardFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(forwardFlow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Messenger)]
        public void Messenger(IConnection connection)
        {
            connection.Write(serializer.Serialize(messengerFlow.GetFlows()));
        }

        [MessengerId((ushort)FlowMessengerIds.Socks5)]
        public void Socks5(IConnection connection)
        {
            socks5Flow.Update();
            Socks5FlowRequestInfo info = serializer.Deserialize<Socks5FlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(socks5Flow.GetFlows(info)));
        }

        [MessengerId((ushort)FlowMessengerIds.Tunnel)]
        public void Tunnel(IConnection connection)
        {
            tunnelFlow.Update();
            TunnelFlowRequestInfo info = serializer.Deserialize<TunnelFlowRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            connection.Write(serializer.Serialize(tunnelFlow.GetFlows(info)));
        }
    }
}
