using linker.messenger.signin;
using LiteDB;

namespace linker.messenger.store.file.signIn
{
    public sealed class SignInServerStore : ISignInServerStore
    {
        public int CleanDays => fileConfig.Data.Server.SignIn.CleanDays;

        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<SignCacheInfo> liteCollection;
        private readonly FileConfig fileConfig;
        public SignInServerStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<SignCacheInfo>("signs");
            this.fileConfig = fileConfig;
        }

        public bool ValidateSecretKey(string key)
        {
            return string.IsNullOrWhiteSpace(fileConfig.Data.Server.SignIn.SecretKey) || fileConfig.Data.Server.SignIn.SecretKey == key;
        }
        public void SetSecretKey(string secretKey)
        {
            fileConfig.Data.Server.SignIn.SecretKey = secretKey;
            fileConfig.Data.Update();
        }
        public void SetCleanDays(int days)
        {
            fileConfig.Data.Server.SignIn.CleanDays = days;
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
            return liteCollection.FindAll();
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


    }
}
