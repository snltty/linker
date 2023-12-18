using MemoryPack;

namespace cmonitor.client.reports.system
{
    public interface ISystem
    {
        public ReportDriveInfo[] GetAllDrives();

        public Dictionary<string, SystemOptionKeyInfo> OptionKeys();
        public string OptionValues();
        public void OptionUpdate(SystemOptionUpdateInfo optionUpdateInfo);

        public bool Password(PasswordInputInfo command);


        public double GetCpu();
        public double GetMemory();
    }


    public sealed class ReportDriveInfo
    {
        public string Name { get; set; }
        public long Free { get; set; }
        public long Total { get; set; }
    }

    public sealed class SystemOptionKeyInfo
    {
        public string Desc { get; set; }
        public ushort Index { get; set; }
    }

    [MemoryPackable]
    public sealed partial class PasswordInputInfo
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
