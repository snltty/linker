
namespace linker.messenger.tuntap
{
    public interface ITuntapSystemInformation
    {
        public string Get()
        {
            string desc = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            string fnos = (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_FNOS")) == false ? "fnos" : "");
            string docker = (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNLTTY_LINKER_IS_DOCKER")) == false ? "Docker" : "");

            return $"{desc} {fnos} {docker}";
        }
    }
    public sealed class TuntapSystemInformation : ITuntapSystemInformation { }
}
