using cmonitor.db;
using LiteDB;

namespace cmonitor.plugins.hijack.db
{
    public sealed class HijackDB : IHijackDB
    {
        ILiteCollection<HijackRuleUserInfo> collectionRule;
        ILiteCollection<HijackProcessUserInfo> collectionProcess;
        private readonly DBfactory dBfactory;
        public HijackDB(DBfactory dBfactory)
        {
            this.dBfactory = dBfactory;
            collectionRule = dBfactory.GetCollection<HijackRuleUserInfo>("hijack-rule");
            collectionProcess = dBfactory.GetCollection<HijackProcessUserInfo>("hijack-process");
        }

        public bool AddProcess(HijackProcessUserInfo hijackProcessUserInfo)
        {
            HijackProcessUserInfo old = collectionProcess.FindOne(c => c.UserName == hijackProcessUserInfo.UserName);
            if (old != null)
            {
                old.Data = hijackProcessUserInfo.Data;
                collectionProcess.Update(old);
            }
            else
            {
                collectionProcess.Insert(hijackProcessUserInfo);
            }
            dBfactory.Confirm();
            return true;
        }

        public bool AddRule(HijackRuleUserInfo hijackRuleUserInfo)
        {
            HijackRuleUserInfo old = collectionRule.FindOne(c => c.UserName == hijackRuleUserInfo.UserName);
            if (old != null)
            {
                old.Data = hijackRuleUserInfo.Data;
                collectionRule.Update(old);
            }
            else
            {
                collectionRule.Insert(hijackRuleUserInfo);
            }
            return true;
        }

        public List<HijackProcessUserInfo> GetProcess()
        {
            return collectionProcess.FindAll().ToList();
        }

        public List<HijackRuleUserInfo> GetRule()
        {
            return collectionRule.FindAll().ToList();
        }
    }
}
