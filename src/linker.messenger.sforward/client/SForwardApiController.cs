using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.api;
using linker.messenger.node;
using linker.messenger.sforward.messenger;
using linker.messenger.sforward.server;
using linker.messenger.signin;

namespace linker.messenger.sforward.client
{
    public sealed class SForwardApiController : IApiController
    {
        private readonly SForwardClientTransfer forwardTransfer;
        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISForwardClientStore sforwardClientStore;
        private readonly ISerializer serializer;
        private readonly IAccessStore accessStore;
        private readonly SForwardClientTestTransfer sforwardClientTestTransfer;

        public SForwardApiController(SForwardClientTransfer forwardTransfer, IMessengerSender messengerSender,
            SignInClientState signInClientState, ISignInClientStore signInClientStore,
            ISForwardClientStore sforwardClientStore, ISerializer serializer, IAccessStore accessStore,
            SForwardClientTestTransfer sforwardClientTestTransfer)
        {
            this.forwardTransfer = forwardTransfer;
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.signInClientStore = signInClientStore;
            this.sforwardClientStore = sforwardClientStore;
            this.serializer = serializer;
            this.accessStore = accessStore;
            this.sforwardClientTestTransfer = sforwardClientTestTransfer;
        }

        public List<SForwardServerNodeStoreInfo> Subscribe(ApiControllerParamsInfo param)
        {
            sforwardClientTestTransfer.Subscribe();
            return sforwardClientTestTransfer.Nodes;
        }

        /// <summary>
        /// 获取穿透列表
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<List<SForwardInfo>> Get(ApiControllerParamsInfo param)
        {
            if (param.Content == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardShowSelf) == false) return new List<SForwardInfo>();
                return sforwardClientStore.Get().ToList();
            }

            if (accessStore.HasAccess(AccessValue.ForwardShowOther) == false) return new List<SForwardInfo>();
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.GetForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return serializer.Deserialize<List<SForwardInfo>>(resp.Data.Span);
            }
            return new List<SForwardInfo>();
        }

        /// <summary>
        /// 添加穿透
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> AddClient(ApiControllerParamsInfo param)
        {
            SForwardAddForwardInfo info = param.Content.DeJson<SForwardAddForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Add(info.Data);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.AddClientForward,
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
            SForwardRemoveForwardInfo info = param.Content.DeJson<SForwardRemoveForwardInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                if (accessStore.HasAccess(AccessValue.ForwardSelf) == false) return false;
                return forwardTransfer.Remove(info.Id);
            }
            if (accessStore.HasAccess(AccessValue.ForwardOther) == false) return false;
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.RemoveClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> Start(ApiControllerParamsInfo param)
        {
            SForwardRemoveForwardInfo info = param.Content.DeJson<SForwardRemoveForwardInfo>();
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
                MessengerId = (ushort)SForwardMessengerIds.StartClientForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return true;
        }
        public async Task<bool> Stop(ApiControllerParamsInfo param)
        {
            SForwardRemoveForwardInfo info = param.Content.DeJson<SForwardRemoveForwardInfo>();
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
                MessengerId = (ushort)SForwardMessengerIds.StopClientForward,
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
                MessengerId = (ushort)SForwardMessengerIds.TestClientForward,
                Payload = serializer.Serialize(param.Content)
            }).ConfigureAwait(false);
            return true;
        }


        public async Task<string> Share(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.ShareForward,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }
        public async Task<string> Import(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.Import,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }
        public async Task<string> Remove(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.Remove,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK ? serializer.Deserialize<string>(resp.Data.Span) : $"network error:{resp.Code}";
        }

        public async Task<bool> Update(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.UpdateForward,
                Payload = serializer.Serialize(param.Content.DeJson<SForwardServerNodeStoreInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        public async Task<bool> Upgrade(ApiControllerParamsInfo param)
        {
            KeyValueInfo<string, string> info = param.Content.DeJson<KeyValueInfo<string, string>>();

            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.UpgradeForward,
                Payload = serializer.Serialize(new KeyValuePair<string, string>(info.Key, info.Value))
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> Exit(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.ExitForward,
                Payload = serializer.Serialize(param.Content)
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }


        public async Task<MastersResponseInfo> Masters(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.MastersForward,
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
                MessengerId = (ushort)SForwardMessengerIds.DenysForward,
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
                MessengerId = (ushort)SForwardMessengerIds.DenysAddForward,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyAddInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> DenysDel(ApiControllerParamsInfo param)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)SForwardMessengerIds.DenysDelForward,
                Payload = serializer.Serialize(param.Content.DeJson<MasterDenyDelInfo>())
            });
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
    }
}
