using cmonitor.client;
using cmonitor.client.report;
using cmonitor.config;
using cmonitor.libs;
using cmonitor.plugins.snatch.messenger;
using cmonitor.server;
using common.libs;
using System.Text;

namespace cmonitor.plugins.snatch.report
{
    public sealed class SnatchReport : IClientReport
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
            Init();
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        private void Init()
        {
            shareMemory.AddAttributeAction((int)ShareMemoryIndexs.SnatchQuestion, (attribute) =>
            {
                if ((attribute & ShareMemoryAttribute.Updated) == ShareMemoryAttribute.Updated)
                {
                    byte[] bytes = shareMemory.ReadValueArray((int)ShareMemoryIndexs.SnatchAnswer);
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
            bool questionRunning = shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.SnatchQuestion, ShareMemoryAttribute.Running);
            bool answerRunning = shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.SnatchAnswer, ShareMemoryAttribute.Running);
            if (questionRunning || answerRunning) return;

            Task.Run(async () =>
            {
                try
                {
                    shareMemory.AddAttribute((int)ShareMemoryIndexs.SnatchQuestion, ShareMemoryAttribute.Closed);
                    shareMemory.AddAttribute((int)ShareMemoryIndexs.SnatchAnswer, ShareMemoryAttribute.Closed);

                    await Task.Delay(1000);

                    shareMemory.Update((int)ShareMemoryIndexs.SnatchQuestion, Encoding.UTF8.GetBytes("SQ"), snatchQuestionInfo.ToBytes(),
                        addAttri: ShareMemoryAttribute.HiddenForList, removeAttri: ShareMemoryAttribute.All);
                    shareMemory.Update((int)ShareMemoryIndexs.SnatchAnswer, Encoding.UTF8.GetBytes("SA"), new SnatchAnswerInfo
                    {
                        UserName = snatchQuestionInfo.UserName,
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
            bool questionRunning = shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.SnatchQuestion, ShareMemoryAttribute.Running);
            bool answerRunning = shareMemory.ReadAttributeEqual((int)ShareMemoryIndexs.SnatchAnswer, ShareMemoryAttribute.Running);
            if (questionRunning && answerRunning)
            {
                shareMemory.Update((int)ShareMemoryIndexs.SnatchQuestion, Encoding.UTF8.GetBytes("SQ"), snatchQuestionInfo.ToBytes(), addAttri: ShareMemoryAttribute.HiddenForList);
            }
        }
        public void Remove()
        {
            shareMemory.AddAttribute((int)ShareMemoryIndexs.SnatchQuestion, ShareMemoryAttribute.Closed);
            shareMemory.AddAttribute((int)ShareMemoryIndexs.SnatchAnswer, ShareMemoryAttribute.Closed);
            shareMemory.Update((int)ShareMemoryIndexs.SnatchQuestion, "SQ", string.Empty);
            shareMemory.Update((int)ShareMemoryIndexs.SnatchAnswer, "SA", string.Empty);
        }
    }
}
