using cmonitor.libs;
using cmonitor.service;
using cmonitor.service.messengers.snatch;
using common.libs;
using System.Text;

namespace cmonitor.client.reports.snatch
{
    public sealed class SnatchReport : IReport
    {
        public string Name => "Snatch";

        private readonly ISnatch snatch;
        private readonly ShareMemory shareMemory;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;

        public SnatchReport(Config config, ISnatch snatch, ShareMemory shareMemory, MessengerSender messengerSender, ClientSignInState clientSignInState)
        {
            this.snatch = snatch;
            this.shareMemory = shareMemory;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            if (config.IsCLient)
            {
                Init();
            }
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        private void Init()
        {
            shareMemory.AddAttributeAction(Config.ShareSnatchAnswerIndex, (ShareMemoryAttribute attribute) =>
            {
                if ((attribute & ShareMemoryAttribute.Updated) == ShareMemoryAttribute.Updated)
                {
                    byte[] bytes = shareMemory.ReadValueArray(Config.ShareSnatchAnswerIndex);
                    if (bytes.Length > 0)
                    {
                        _ = messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = clientSignInState.Connection,
                            MessengerId = (ushort)SnatchMessengerIds.UpdateAnswer,
                            Payload = bytes
                        });
                    }
                }
            });
        }
        public void Add(SnatchQuestionInfo snatchQuestionInfo)
        {
            bool questionRunning = shareMemory.ReadAttributeEqual(Config.ShareSnatchQuestionIndex, ShareMemoryAttribute.Running);
            bool answerRunning = shareMemory.ReadAttributeEqual(Config.ShareSnatchAnswerIndex, ShareMemoryAttribute.Running);
            if (questionRunning || answerRunning) return;

            Task.Run(async () =>
            {
                try
                {
                    shareMemory.AddAttribute(Config.ShareSnatchQuestionIndex, ShareMemoryAttribute.Closed);
                    shareMemory.AddAttribute(Config.ShareSnatchAnswerIndex, ShareMemoryAttribute.Closed);

                    await Task.Delay(100);

                    shareMemory.Update(Config.ShareSnatchQuestionIndex, Encoding.UTF8.GetBytes("SQ"), snatchQuestionInfo.ToBytes(),
                        addAttri: ShareMemoryAttribute.HiddenForList, removeAttri: ShareMemoryAttribute.All);
                    shareMemory.Update(Config.ShareSnatchAnswerIndex, Encoding.UTF8.GetBytes("SA"), new SnatchAnswerInfo
                    {
                        Name = snatchQuestionInfo.Name,
                        Result = false,
                        ResultStr = string.Empty,
                        State = SnatchState.Ask,
                        Time = 0,
                        Times = 0
                    }.ToBytes(), addAttri: ShareMemoryAttribute.HiddenForList, removeAttri: ShareMemoryAttribute.All);
                    snatch.StartUp(snatchQuestionInfo);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            });
        }
        public void Update(SnatchQuestionInfo snatchQuestionInfo)
        {
            bool questionRunning = shareMemory.ReadAttributeEqual(Config.ShareSnatchQuestionIndex, ShareMemoryAttribute.Running);
            bool answerRunning = shareMemory.ReadAttributeEqual(Config.ShareSnatchAnswerIndex, ShareMemoryAttribute.Running);
            if (questionRunning && answerRunning)
            {
                shareMemory.Update(Config.ShareSnatchQuestionIndex, Encoding.UTF8.GetBytes("SQ"), snatchQuestionInfo.ToBytes(), addAttri: ShareMemoryAttribute.HiddenForList);
            }
        }
        public void Remove()
        {
            shareMemory.AddAttribute(Config.ShareSnatchQuestionIndex, ShareMemoryAttribute.Closed);
            shareMemory.AddAttribute(Config.ShareSnatchAnswerIndex, ShareMemoryAttribute.Closed);
            shareMemory.Update(Config.ShareSnatchQuestionIndex, "SnatchQuestion", string.Empty);
            shareMemory.Update(Config.ShareSnatchAnswerIndex, "SnatchAnswer", string.Empty);
        }
    }
}
