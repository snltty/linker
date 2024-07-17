using linker.plugins.updater.config;

namespace linker.plugins.updater.config
{
    public sealed class UpdaterConfigInfo
    {

    }
}


namespace linker.config
{
    public sealed partial class ConfigClientInfo
    {
        public UpdaterConfigInfo Updater { get; set; } = new UpdaterConfigInfo();
    }
}