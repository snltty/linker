
namespace linker.messenger.tuntap
{
    public interface ITuntapSystemInformation
    {
        public string Get()
        {
            return $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")) == false ? "Docker" : "")}";
        }
    }
    public sealed class TuntapSystemInformation : ITuntapSystemInformation { }
}
