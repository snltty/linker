using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace linker.messenger.tuntap
{
    public interface ISystemInformation
    {
        public string Get()
        {
            return $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")) == false ? "Docker" : "")}";
        }
    }
    public sealed class SystemInformation: ISystemInformation { }
}
