using cmonitor.plugins.signin.messenger;
using cmonitor.plugins.snatch.report;
using cmonitor.server;

namespace cmonitor.plugins.snatch.messenger
{
    public sealed class SnatchClientMessenger : IMessenger
    {
        private readonly SnatchReport snatchReport;

        public SnatchClientMessenger(SnatchReport snatchReport)
        {
            this.snatchReport = snatchReport;
        }

        [MessengerId((ushort)SnatchMessengerIds.AddQuestion)]
        public void AddQuestion(IConnection connection)
        {
            SnatchQuestionInfo question = SnatchQuestionInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            snatchReport.Add(question);
        }
        [MessengerId((ushort)SnatchMessengerIds.UpdateQuestion)]
        public void UpdateQuestion(IConnection connection)
        {
            SnatchQuestionInfo question = SnatchQuestionInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            snatchReport.Update(question);
        }
        [MessengerId((ushort)SnatchMessengerIds.RemoveQuestion)]
        public void RemoveQuestion(IConnection connection)
        {
            snatchReport.Remove();
        }

    }


    public sealed class SnatchServerMessenger : IMessenger
    {
        private readonly ISnatachCaching snatachCaching;
        private readonly SignCaching signCaching;
        private readonly MessengerSender messengerSender;

        public SnatchServerMessenger( ISnatachCaching snatachCaching, SignCaching signCaching, MessengerSender messengerSender)
        {
            this.snatachCaching = snatachCaching;
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
        }

       
        [MessengerId((ushort)SnatchMessengerIds.UpdateAnswer)]
        public void UpdateAnswer(IConnection connection)
        {
            SnatchAnswerInfo answerInfo = SnatchAnswerInfo.DeBytes(connection.ReceiveRequestWrap.Payload);
            snatachCaching.Update(connection.Id, answerInfo);

            Task.Run(async () =>
            {
                if (snatachCaching.Get(answerInfo.UserName, connection.Id, out SnatchAnswerInfo info))
                {
                    byte[] bytes = info.Question.ToBytes();
                    SnatchAnswerInfo[] answers = snatachCaching.Get(info);
                    foreach (var item in answers)
                    {
                        if (signCaching.TryGet(item.MachineId, out SignCacheInfo cache))
                        {
                            await messengerSender.SendOnly(new MessageRequestWrap
                            {
                                Connection = cache.Connection,
                                MessengerId = (ushort)SnatchMessengerIds.UpdateQuestion,
                                Payload = bytes
                            });
                        }
                    }
                }
            });
        }
    }
}
