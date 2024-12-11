using linker.client.config;
using linker.config;
using linker.libs;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.plugins.updater.messenger;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.plugins.updater
{
    public sealed class UpdaterClientTransfer
    {
        private UpdateInfo updateInfo = new UpdateInfo();
        private ConcurrentDictionary<string, UpdateInfo> updateInfos = new ConcurrentDictionary<string, UpdateInfo>();
        private ConcurrentDictionary<string, LastTicksManager> subscribes = new ConcurrentDictionary<string, LastTicksManager>();

        public string SecretKey => fileConfig.Data.Client.Updater.SecretKey;

        private readonly FileConfig fileConfig;
        private readonly IMessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly UpdaterHelper updaterHelper;
        private readonly ClientConfigTransfer clientConfigTransfer;
        private readonly UpdaterCommonTransfer updaterCommonTransfer;

        private readonly RunningConfig running;

        public VersionManager Version { get; } = new VersionManager();

        public UpdaterClientTransfer(FileConfig fileConfig, IMessengerSender messengerSender, ClientSignInState clientSignInState, UpdaterHelper updaterHelper, RunningConfig running, ClientConfigTransfer clientConfigTransfer, UpdaterCommonTransfer updaterCommonTransfer)
        {
            this.fileConfig = fileConfig;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.updaterHelper = updaterHelper;
            this.running = running;
            this.clientConfigTransfer = clientConfigTransfer;
            this.updaterCommonTransfer = updaterCommonTransfer;

            clientSignInState.NetworkFirstEnabledHandle += Init;
           
        }
        private void Init()
        {
            LoadTask();
            UpdateTask();
            updateInfo.Update();
        }

        public string GetSecretKey()
        {
            return fileConfig.Data.Client.Updater.SecretKey;
        }
        public void SetSecretKey(string key)
        {
            fileConfig.Data.Client.Updater.SecretKey = key;
            fileConfig.Data.Update();
        }

        /// <summary>
        /// 所有客户端的更新信息
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, UpdateInfo> Get()
        {
            return updateInfos;
        }
        /// <summary>
        /// 确认更新
        /// </summary>
        public void Confirm(string version)
        {
            updaterHelper.Confirm(updateInfo, version);
        }
        /// <summary>
        /// 来自别的客户端的更新信息
        /// </summary>
        /// <param name="info"></param>
        public void Update(UpdateInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.MachineId) == false)
            {
                updateInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
                Version.Add();
            }
        }


        private readonly LastTicksManager lastTicksManager = new LastTicksManager();
        public void Subscribe(string machineId)
        {
            if (subscribes.TryGetValue(machineId, out LastTicksManager _lastTicksManager) == false)
            {
                _lastTicksManager = new LastTicksManager();
                subscribes.TryAdd(machineId, _lastTicksManager);
                updateInfo.Update();
            }

            //距离上次订阅超过一分钟，需要立即更新一次
            bool needUpdate = _lastTicksManager.DiffGreater(60 * 1000);
            _lastTicksManager.Update();
            lastTicksManager.Update();

            if (needUpdate)
            {
                updateInfo.Update();
            }
        }

        public void Check()
        {
            _ = updaterHelper.GetUpdateInfo(updateInfo);
        }

        private void UpdateTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (updateInfo.Updated)
                {
                    updateInfo.MachineId = clientConfigTransfer.Id;
                    string[] machines = subscribes.Where(c => c.Value.DiffLessEqual(15000)).Select(c => c.Key).ToArray();
                    if (machines.Length > 0)
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = clientSignInState.Connection,
                            MessengerId = (ushort)UpdaterMessengerIds.UpdateForward,
                            Payload = MemoryPackSerializer.Serialize(new UpdateClientInfo { ToMachines = machines, Info = updateInfo }),
                        });
                    }
                    Update(updateInfo);
                }
                return true;
            }, () => lastTicksManager.DiffLessEqual(5000) ? 1000 : 15000);

        }
        private void LoadTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                await updaterHelper.GetUpdateInfo(updateInfo);
                return true;
            }, () => updaterCommonTransfer.UpdateIntervalSeconds*1000);
        }
    }

    [MemoryPackable]
    public sealed partial class UpdateClientInfo
    {
        public string[] ToMachines { get; set; }
        public UpdateInfo Info { get; set; }
    }

}
