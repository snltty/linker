using linker.libs;
using linker.messenger.signin;
using LiteDB;

namespace linker.messenger.store.file.signIn
{
    public sealed class SignInServerStore : ISignInServerStore
    {
        public int CleanDays => fileConfig.Data.Server.SignIn.CleanDays;
        public bool Enabled => fileConfig.Data.Server.SignIn.Enabled;
        public bool Anonymous => fileConfig.Data.Server.SignIn.Anonymous;

        public string[] Hosts => fileConfig.Data.Server.Hosts;

        private readonly ILiteCollection<SignCacheInfo> liteCollection;
        private readonly FileConfig fileConfig;
        public SignInServerStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            liteCollection = dBfactory.GetCollection<SignCacheInfo>("signs");
            this.fileConfig = fileConfig;

        }


        public bool ValidateSuper(string key, string password)
        {
            return fileConfig.Data.Server.SignIn.SuperKey == key && fileConfig.Data.Server.SignIn.SuperPassword == password;
        }
        public void SetSuper(string key, string password)
        {
            fileConfig.Data.Server.SignIn.SuperKey = key;
            fileConfig.Data.Server.SignIn.SuperPassword = password;
            fileConfig.Data.Update();
        }
        public void SetCleanDays(int days)
        {
            fileConfig.Data.Server.SignIn.CleanDays = days;
            fileConfig.Data.Update();
        }
        public void SetEnabled(bool enabled)
        {
            fileConfig.Data.Server.SignIn.Enabled = enabled;
            fileConfig.Data.Update();
        }
        public void SetAnonymous(bool anonymous)
        {
            fileConfig.Data.Server.SignIn.Anonymous = anonymous;
            fileConfig.Data.Update();
        }

        public void Confirm()
        {
            fileConfig.Data.Update();
        }

        public bool Delete(string id)
        {
            return liteCollection.Delete(id);
        }

        public SignCacheInfo Find(string id)
        {
            return liteCollection.FindOne(id);
        }

        public IEnumerable<SignCacheInfo> Find()
        {
            return liteCollection.FindAll().ToList().ToList();
        }

        public string Insert(SignCacheInfo value)
        {
            return liteCollection.Insert(value);
        }

        public string NewId()
        {
            return ObjectId.NewObjectId().ToString();
        }

        public bool Update(SignCacheInfo value)
        {
            return liteCollection.Update(value);
        }
        public string[] Exp(string id)
        {
            try
            {
                long start = Environment.TickCount64;
                liteCollection.UpdateMany(p => new SignCacheInfo { LastSignIn = DateTime.Now }, c => c.Id == id);
                long end = Environment.TickCount64;
                if (end - start > 1000)
                {
                    LoggerHelper.Instance.Error("SignInServerStore Exp UpdateMany too long {0}ms", end - start);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error("SignInServerStore Exp UpdateMany {0}", ex);
            }
            return fileConfig.Data.Server.Hosts;
        }

    }
}
