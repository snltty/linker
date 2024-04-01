using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmonitor.client.ruleConfig
{
    public interface IRuleConfig
    {
        public T Get<T>(T defaultValue);
        public T Get<T>(string name, T defaultValue);
        public void Set<T>(T data);
    }
}
