using linker.libs;
using linker.libs.extends;
using linker.libs.web;
using linker.messenger.signin;

namespace linker.messenger.plan
{
    /// <summary>
    /// 中继管理接口
    /// </summary>
    public sealed class PlanApiController : IApiController
    {
        private readonly SignInClientState signInClientState;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;
        private readonly ISignInClientStore signInClientStore;
        private readonly PlanTransfer planTransfer;

        public PlanApiController( SignInClientState signInClientState, IMessengerSender messengerSender, ISerializer serializer, ISignInClientStore signInClientStore, PlanTransfer planTransfer)
        {
            this.signInClientState = signInClientState;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
            this.signInClientStore = signInClientStore;
            this.planTransfer = planTransfer;
        }
        public async Task<List<PlanInfo>> Get(ApiControllerParamsInfo param)
        {
            PlanGetInfo info = param.Content.DeJson<PlanGetInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                return planTransfer.Get(info.Category).ToList();
            }
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)PlanMessengerIds.GetForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);

            if(resp.Code == MessageResponeCodes.OK)
            {
               return serializer.Deserialize<List<PlanInfo>>(resp.Data.Span);
            }
            return new List<PlanInfo>();
        }
        public async Task<bool> Add(ApiControllerParamsInfo param)
        {
            PlanAddInfo info = param.Content.DeJson<PlanAddInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                return planTransfer.Add(info.Plan);
            }
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)PlanMessengerIds.AddForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
        public async Task<bool> Remove(ApiControllerParamsInfo param)
        {
            PlanRemoveInfo info = param.Content.DeJson<PlanRemoveInfo>();
            if (info.MachineId == signInClientStore.Id)
            {
                return planTransfer.Remove(info.PlanId);
            }
            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)PlanMessengerIds.RemoveForward,
                Payload = serializer.Serialize(info)
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

    }
}
