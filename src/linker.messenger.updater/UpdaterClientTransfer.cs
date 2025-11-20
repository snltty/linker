using linker.libs;
using System.Collections.Concurrent;
using linker.messenger.signin;
using linker.libs.timer;

namespace linker.messenger.updater
{
    public sealed class UpdaterClientTransfer
    {
        private UpdaterInfo updateInfo = new UpdaterInfo();
        private ConcurrentDictionary<string, UpdaterInfo170> updateInfos = new ConcurrentDictionary<string, UpdaterInfo170>();
        private ConcurrentDictionary<string, LastTicksManager> subscribes = new ConcurrentDictionary<string, LastTicksManager>();

        private readonly IMessengerSender messengerSender;
        private readonly SignInClientState signInClientState;
        private readonly UpdaterHelper updaterHelper;
        private readonly ISignInClientStore signInClientStore;
        private readonly ISerializer serializer;
        private readonly IUpdaterClientStore updaterClientStore;

        public VersionManager Version { get; } = new VersionManager();

        public UpdaterClientTransfer(IMessengerSender messengerSender, SignInClientState signInClientState, UpdaterHelper updaterHelper,
            ISignInClientStore signInClientStore, ISerializer serializer, IUpdaterClientStore updaterClientStore)
        {
            this.messengerSender = messengerSender;
            this.signInClientState = signInClientState;
            this.updaterHelper = updaterHelper;
            this.signInClientStore = signInClientStore;
            this.serializer = serializer;
            this.updaterClientStore = updaterClientStore;

            signInClientState.OnSignInSuccessFirstTime += Init;

        }
        private void Init()
        {
            UpdateTask();
            updateInfo.Update();
        }

        /// <summary>
        /// 所有客户端的更新信息
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, UpdaterInfo170> Get()
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
        public void Update(UpdaterInfo170 info)
        {
            if (string.IsNullOrWhiteSpace(info.MachineId) == false)
            {
                updateInfos.AddOrUpdate(info.MachineId, info, (a, b) => info);
                Version.Increment();
            }
        }
        public void Update(UpdaterInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.MachineId) == false)
            {
                UpdaterInfo170 _info = new UpdaterInfo170
                {
                    Version = info.Version,
                    Status = info.Status,
                    Length = info.Length,
                    Current = info.Current,
                    MachineId = info.MachineId,

                };
                updateInfos.AddOrUpdate(_info.MachineId, _info, (a, b) => _info);
                Version.Increment();
            }
        }


        private readonly LastTicksManager lastTicksManager = new LastTicksManager();

        /// <summary>
        /// 订阅更新信息
        /// </summary>
        /// <param name="machineId"></param>
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
        public void Subscribe()
        {
            if (updateInfo.Status == UpdaterStatus.Downloading || updateInfo.Status == UpdaterStatus.Extracting)
            {
                updateInfo.MachineId = signInClientStore.Id;
                Update(updateInfo);
                Version.Increment();
            }
        }

        private void UpdateTask()
        {
            TimerHelper.SetIntervalLong(async () =>
            {
                await GetUpdateInfo().ConfigureAwait(false);
                if (updateInfo.Updated)
                {
                    updateInfo.MachineId = signInClientStore.Id;
                    string[] machines = subscribes.Where(c => c.Value.DiffLessEqual(15000)).Select(c => c.Key).ToArray();
                    if (machines.Length > 0)
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = signInClientState.Connection,
                            MessengerId = (ushort)UpdaterMessengerIds.UpdateForward170,
                            Payload = serializer.Serialize(new UpdaterClientInfo170
                            {
                                ToMachines = machines,
                                Info = new UpdaterInfo170
                                {
                                    Current = updateInfo.Current,
                                    Length = updateInfo.Length,
                                    Status = updateInfo.Status,
                                    MachineId = updateInfo.MachineId
                                }
                            }),
                        }).ConfigureAwait(false);
                    }
                    Update(updateInfo);
                }
            }, () => lastTicksManager.DiffLessEqual(5000) ? 3000 : 15000);

        }
        public void Check()
        {
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Info($"check update");
            _ = GetUpdateInfo();
        }
        private async Task GetUpdateInfo()
        {
            if (updateInfo.Status > UpdaterStatus.Checked) return;

            MessageResponeInfo resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = signInClientState.Connection,
                MessengerId = (ushort)UpdaterMessengerIds.UpdateServer186,
            }).ConfigureAwait(false);
            if (resp.Code == MessageResponeCodes.OK && resp.Data.Length > 0)
            {
                UpdaterInfo170 info = serializer.Deserialize<UpdaterInfo170>(resp.Data.Span);

                //服务端不是已经开始下载，本地也不是已经开始下载，就更新一下本地状态
                if (info.Status < UpdaterStatus.Downloading && updateInfo.Status < UpdaterStatus.Downloading)
                {
                    updateInfo.Status = info.Status;
                    updateInfo.Version = info.Version;
                    updateInfo.Update();
                }

                //本地不是已经开始下载，开启了自动更新，且版本不一样
                if (updateInfo.Status < UpdaterStatus.Downloading && (updaterClientStore.Info.Sync2Server || info.Sync2Server) && info.ServerVersion != VersionHelper.Version)
                {
                    updaterHelper.Confirm(updateInfo, info.ServerVersion);
                }
            }
        }
    }
}
