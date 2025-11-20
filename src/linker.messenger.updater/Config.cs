using System.Collections.Concurrent;
using System.Text.Json.Serialization;
namespace linker.messenger.updater
{
    public sealed partial class UpdaterConfirmInfo
    {
        public string MachineId { get; set; }
        public string Version { get; set; }
        public bool GroupAll { get; set; }
        public bool All { get; set; }
    }
    public sealed partial class UpdaterConfirmServerInfo
    {
        public string Version { get; set; }
    }

    public sealed class UpdaterConfigClientInfo
    {
        /// <summary>
        /// 与服务器同步
        /// </summary>
        public bool Sync2Server { get; set; } = false;
    }
    public sealed class UpdaterConfigServerInfo
    {
        /// <summary>
        /// 与服务器同步
        /// </summary>
        public bool Sync2Server { get; set; } = false;
    }


    public sealed class UpdaterListInfo
    {
        public ConcurrentDictionary<string, UpdaterInfo170> List { get; set; }
        public ulong HashCode { get; set; }
    }

    public sealed partial class UpdaterClientInfo
    {
        public string[] ToMachines { get; set; }
        public UpdaterInfo Info { get; set; }
    }
    public sealed partial class UpdaterClientInfo170
    {
        public string[] ToMachines { get; set; }
        public UpdaterInfo170 Info { get; set; }
    }

    public sealed partial class UpdaterInfo
    {
        public string Version { get; set; }
        public string[] Msg { get; set; }
        public string DateTime { get; set; }

        public string MachineId { get; set; }

        private ulong counter = 0;
        [JsonIgnore]
        public bool Updated => Interlocked.And(ref counter, 0x0) > 0;


        private UpdaterStatus status = UpdaterStatus.None;
        public UpdaterStatus Status { get => status; set { status = value; Interlocked.Increment(ref counter); } }

        private long length = 0;
        public long Length { get => length; set { length = value; Interlocked.Increment(ref counter); } }
        private long current = 0;
        public long Current { get => current; set { current = value; Interlocked.Increment(ref counter); } }

        public void Update()
        {
            Interlocked.Increment(ref counter);
        }
    }
    public partial class UpdaterInfo170
    {
        public string MachineId { get; set; }
        public string Version { get; set; }
        public UpdaterStatus Status { get; set; }
        public long Length { get; set; }
        public long Current { get; set; }

        public string ServerVersion { get; set; }
        public bool Sync2Server { get; set; }
    }

    public enum UpdaterStatus : byte
    {
        None = 0,
        Checking = 1,
        Checked = 2,
        Downloading = 3,
        Downloaded = 4,
        Extracting = 5,
        Extracted = 6
    }
}