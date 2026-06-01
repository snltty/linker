using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.node;
using linker.messenger.reverse.messenger;
using linker.messenger.reverse.server;
using linker.messenger.signin;

namespace linker.messenger.reverse.client
{
    public sealed class ReverseApiController : IApiController
    {
        private readonly ReverseClientTransfer forwardTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly IReverseClientStore ReverseClientStore;
        private readonly ISerializer serializer;
        private readonly IAccessStore accessStore;
        private readonly ReverseClientTestTransfer ReverseClientTestTransfer;

        public ReverseApiController(ReverseClientTransfer forwardTransfer, IMessengerSender messengerSender,
            SignInClientState signInClientState, ISignInClientStore signInClientStore,
            IReverseClientStore ReverseClientStore, ISerializer serializer, IAccessStore accessStore,
            ReverseClientTestTransfer ReverseClientTestTransfer)
        {
            this.forwardTransfer = forwardTransfer;
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.ReverseClientStore = ReverseClientStore;
            this.serializer = serializer;
            this.accessStore = accessStore;
            this.ReverseClientTestTransfer = ReverseClientTestTransfer;
        }

        public List<ReverseServerNodeStoreInfo> Subscribe(ApiControllerParamsInfo param)
        {
            ReverseClientTestTransfer.Subscribe();
            return ReverseClientTestTransfer.Nodes;
        }

        /// <summary>
        /// 获取穿透列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<ReverseInfo>> Get(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardShowSelf) == false) return new List<ReverseInfo>();
                return ReverseClientStore.Get().ToList();
            }

            if (accessStore.HasAccess(AccessValue.ForwardShowOther) == false) return new List<ReverseInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.GetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<ReverseInfo>>(resp.Data.Span);
            }
            return new List<ReverseInfo>();
        }

        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> AddClient(ApiControllerParamsInfo param)
        {
            ReverseAddForwardInfo info = param.Content.DeJson<ReverseAddForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Add(info.Data);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.AddClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 删除穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> RemoveClient(ApiControllerParamsInfo param)
        {
            ReverseRemoveForwardInfo info = param.Content.DeJson<ReverseRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Remove(info.Id);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.RemoveClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> Start(ApiControllerParamsInfo param)
        {
            ReverseRemoveForwardInfo info = param.Content.DeJson<ReverseRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                forwardTransfer.Start(info.Id);
                return true;
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.StartClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return true;
        }
        public async Task<bool> Stop(ApiControllerParamsInfo param)
        {
            ReverseRemoveForwardInfo info = param.Content.DeJson<ReverseRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                forwardTransfer.Stop(info.Id);
                return true;
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.StopClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// 测试服务
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> TestLocal(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                forwardTransfer.SubscribeTest();
                return true;
            }
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.TestClientForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            return true;
        }


        public async Task<string> Share(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.ShareForward,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }
        public async Task<string> Import(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.Import,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }
        public async Task<string> Remove(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.Remove,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }

        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.UpdateForward,
                Payload = serializer.Serialize(param.Content.DeJson<ReverseServerNodeStoreInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<bool> Upgrade(ApiControllerParamsInfo param)
        {
            KeyValueInfo<string, string> info = param.Content.DeJson<KeyValueInfo<string, string>>();

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.UpgradeForward,
                Payload = serializer.Serialize(new KeyValuePair<string, string>(info.Key, info.Value))
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.ExitForward,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }


        public async Task<MastersResponseInfo> Masters(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.MasterReverse,
                Payload = serializer.Serialize(param.Content.DeJson<MastersRequestInfo>())
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<MastersResponseInfo>(resp.Data.Span);
            }
            return new MastersResponseInfo();
        }
        public async Task<MasterDenyStoreResponseInfo> Denys(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.DenyReverse,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyStoreRequestInfo>())
            });
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<MasterDenyStoreResponseInfo>(resp.Data.Span);
            }
            return new MasterDenyStoreResponseInfo();
        }
        public async Task<bool> DenysAdd(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.DenysAddForward,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyAddInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> DenysDel(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)ReverseMessengerIds.DenysDelForward,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyDelInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
    }
}
