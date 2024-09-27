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

        private readonly FileConfig fileConfig;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly UpdaterHelper updaterHelper;

        private readonly RunningConfig running;

        public VersionManager Version { get; } = new VersionManager();

        public UpdaterClientTransfer(FileConfig fileConfig, MessengerSender messengerSender, ClientSignInState clientSignInState, UpdaterHelper updaterHelper, RunningConfig running)
        {
            this.fileConfig = fileConfig;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.updaterHelper = updaterHelper;
            this.running = running;
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                LoadTask();
                UpdateTask();
                updateInfo.Update();
            };

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


        public void Subscribe(string machineId)
        {
            if (subscribes.TryGetValue(machineId, out LastTicksManager lastTicksManager) == false)
            {
                lastTicksManager = new LastTicksManager();
                subscribes.TryAdd(machineId, lastTicksManager);
            }

            //距离上次订阅超过一分钟，需要立即更新一次
            bool needUpdate = lastTicksManager.Greater(60 * 1000);

            lastTicksManager.Update();

            if (needUpdate)
            {
                updateInfo.Update();
            }
        }

        private void UpdateTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                if (updateInfo.Updated)
                {
                    string[] machines = subscribes.Where(c => c.Value.Less(15000)).Select(c => c.Key).ToArray();
                    if (machines.Length > 0)
                    {
                        updateInfo.MachineId = fileConfig.Data.Client.Id;
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
            }, 1000);

        }
        private void LoadTask()
        {
            TimerHelper.SetInterval(async () =>
            {
                await updaterHelper.GetUpdateInfo(updateInfo);
                return true;
            }, 15000);
        }
    }

    [MemoryPackable]
    public sealed partial class UpdateClientInfo
    {
        public string[] ToMachines { get; set; }
        public UpdateInfo Info { get; set; }
    }

}
