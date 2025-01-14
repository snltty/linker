using linker.messenger.signin;
using LiteDB;

namespace linker.messenger.store.file.signIn
{
    public sealed class SignInServerStore : ISignInServerStore
    {
        public string SecretKey => fileConfig.Data.Server.SignIn.SecretKey;

        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<SignCacheInfo> liteCollection;
        private readonly FileConfig fileConfig;
        public SignInServerStore(Storefactory dBfactory, FileConfig fileConfig)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<SignCacheInfo>("signs");
            this.fileConfig = fileConfig;
        }
        public void SetSecretKey(string secretKey)
        {
            fileConfig.Data.Server.SignIn.SecretKey = secretKey;
        }

        public void Confirm()
        {
            dBfactory.Confirm();
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
