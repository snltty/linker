using System.Collections.Generic;

namespace linker.libs
{
    public interface IStore<T>
    {
        public string NewId();

        public T Find(string id);
        public IEnumerable<T> Find();

        public string Insert(T value);
        public bool Update(T value);
        public bool Delete(string id);

        public void Confirm();
    }
}
