using linker.client;
using linker.client.config;
using linker.config;
using linker.plugins.updater.messenger;
using linker.server;
using MemoryPack;
using System.Collections.Concurrent;

namespace linker.plugins.updater
{
    public sealed class UpdaterClientTransfer
    {
        private UpdateInfo updateInfo = new UpdateInfo();
        private ConcurrentDictionary<string, UpdateInfo> updateInfos = new ConcurrentDictionary<string, UpdateInfo>();

        private readonly FileConfig fileConfig;
        private readonly MessengerSender messengerSender;
        private readonly ClientSignInState clientSignInState;
        private readonly UpdaterHelper updaterHelper;

        private readonly RunningConfig running;
        private readonly RunningConfigTransfer runningConfigTransfer;
        private string configKey = "updater";

        public UpdaterClientTransfer(FileConfig fileConfig, MessengerSender messengerSender, ClientSignInState clientSignInState, UpdaterHelper updaterHelper, RunningConfig running, RunningConfigTransfer runningConfigTransfer)
        {
            this.fileConfig = fileConfig;
            this.messengerSender = messengerSender;
            this.clientSignInState = clientSignInState;
            this.updaterHelper = updaterHelper;
            this.running = running;
            this.runningConfigTransfer = runningConfigTransfer;


            runningConfigTransfer.Setter(configKey, SetSecretKey);
            runningConfigTransfer.Getter(configKey, () => MemoryPackSerializer.Serialize(GetSecretKey()));
            clientSignInState.NetworkFirstEnabledHandle += () =>
            {
                LoadTask();
                UpdateTask();
            };
            
        }
        public string GetSecretKey()
        {
            return running.Data.UpdaterSecretKey;
        }
        public void SetSecretKey(string key)
        {
            running.Data.UpdaterSecretKey = key;
            running.Data.Update();
            runningConfigTransfer.IncrementVersion(configKey);
            SyncKey();
        }
        private void SetSecretKey(Memory<byte> data)
        {
            running.Data.UpdaterSecretKey = MemoryPackSerializer.Deserialize<string>(data.Span);
            running.Data.Update();
        }
        private void SyncKey()
        {
            runningConfigTransfer.Sync(configKey, MemoryPackSerializer.Serialize(GetSecretKey()));
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
            }
        }

        private void UpdateTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (updateInfo.StatusChanged())
                    {
                        updateInfo.MachineId = fileConfig.Data.Client.Id;
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = clientSignInState.Connection,
                            MessengerId = (ushort)UpdaterMessengerIds.UpdateForward,
                            Payload = MemoryPackSerializer.Serialize(updateInfo),
                        });
                        Update(updateInfo);
                    }
                    await Task.Delay(1000);
                }
            });
        }
        private void LoadTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await updaterHelper.GetUpdateInfo(updateInfo);
                    await Task.Delay(60000);
                }
            });
        }
    }

   
}
