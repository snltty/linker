using linker.messenger.signin;
using linker.store;
using LiteDB;

namespace linker.plugins.signIn
{
    public sealed class SignInStore : ISignInStore
    {

        private readonly Storefactory dBfactory;
        private readonly ILiteCollection<SignCacheInfo> liteCollection;
        public SignInStore(Storefactory dBfactory)
        {
            this.dBfactory = dBfactory;
            liteCollection = dBfactory.GetCollection<SignCacheInfo>("signs");
        }

        public void Confirm()
        {
            dBfactory.Confirm();
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
