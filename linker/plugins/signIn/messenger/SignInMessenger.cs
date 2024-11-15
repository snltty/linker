using linker.config;
using MemoryPack;
using linker.plugins.client;
using linker.plugins.messenger;
using linker.libs;
using LiteDB;
using linker.libs.extends;

namespace linker.plugins.signin.messenger
{
    public sealed class SignInClientMessenger : IMessenger
    {
        private readonly ClientSignInTransfer clientSignInTransfer;
        public SignInClientMessenger(FileConfig config, ClientSignInTransfer clientSignInTransfer)
        {
            this.clientSignInTransfer = clientSignInTransfer;
        }

        [MessengerId((ushort)SignInMessengerIds.SetName)]
        public void Name(IConnection connection)
        {
            ConfigSetNameInfo info = MemoryPackSerializer.Deserialize<ConfigSetNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
            clientSignInTransfer.Set(info.NewName);
        }

    }

    public sealed class SignInServerMessenger : IMessenger
    {
        private readonly SignCaching signCaching;
        private readonly FileConfig config;
        private readonly IMessengerSender messengerSender;

        public SignInServerMessenger(SignCaching signCaching, FileConfig config, IMessengerSender messengerSender)
        {
            this.signCaching = signCaching;
            this.config = config;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            connection.Disponse();
            return;
            /*
            SignInfo info = MemoryPackSerializer.Deserialize<SignInfo>(connection.ReceiveRequestWrap.Payload.Span);
            LoggerHelper.Instance.Info($"sign in from {connection.Address}->{info.ToJson()}");

            info.Connection = connection;

            SignInResponseInfo resp = new SignInResponseInfo();
            string msg = await signCaching.Sign(info);
            resp.Status = string.IsNullOrWhiteSpace(msg);
            resp.Msg = msg;
            if (resp.Status)
            {
                connection.Write(MemoryPackSerializer.Serialize(info.MachineId));
            }
            else
            {
                connection.Write(Helper.EmptyArray);
            }
            */
        }

        /// <summary>
        /// v1.3.1版本之后的登录接口
        /// </summary>
        /// <param name="connection"></param>
        [MessengerId((ushort)SignInMessengerIds.SignIn_V_1_3_1)]
        public async Task SignIn_V_1_3_1(IConnection connection)
        {
            SignInfo info = MemoryPackSerializer.Deserialize<SignInfo>(connection.ReceiveRequestWrap.Payload.Span);
            LoggerHelper.Instance.Error($"{info.Version} CompareTo v1.5.0 -> {info.Version.CompareTo("v1.5.0")}");
            if (info.Version.CompareTo("v1.5.0") == -1) //1.5.x
            {
                LoggerHelper.Instance.Error($"{info.MachineName} {info.Version} need v1.5.0+");
                connection.Write(MemoryPackSerializer.Serialize(new SignInResponseInfo { MachineId = string.Empty, Status = false, Msg = "need v1.5.0+" }));
                return;
            }

            LoggerHelper.Instance.Info($"sign in from >=v131 {connection.Address}->{info.ToJson()}");
            info.Connection = connection;
            string msg = await signCaching.Sign(info);

            SignInResponseInfo resp = new SignInResponseInfo
            {
                Status = string.IsNullOrWhiteSpace(msg),
                MachineId = info.MachineId,
                Msg = msg
            };
            connection.Write(MemoryPackSerializer.Serialize(resp.ToJson()));
        }


        [MessengerId((ushort)SignInMessengerIds.SetOrder)]
        public void SetOrder(IConnection connection)
        {
            string[] ids = MemoryPackSerializer.Deserialize<string[]>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                IEnumerable<SignCacheInfo> list = signCaching.Get(cache.GroupId);
                foreach (var item in list)
                {
                    item.Order = uint.MaxValue;
                }

                for (uint i = 0; i < ids.Length; i++)
                {
                    SignCacheInfo item = list.FirstOrDefault(c => c.MachineId == ids[i]);
                    if (item != null)
                    {
                        item.Order = i;
                    }
                }
            }
        }


        [MessengerId((ushort)SignInMessengerIds.List)]
        public void List(IConnection connection)
        {
            SignInListRequestInfo request = MemoryPackSerializer.Deserialize<SignInListRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                IEnumerable<SignCacheInfo> list = signCaching.Get(cache.GroupId).Where(c => c.MachineId != cache.MachineId);
                if (string.IsNullOrWhiteSpace(request.Name) == false)
                {
                    list = list.Where(c => c.Version.Contains(request.Name) || c.IP.ToString().Contains(request.Name) || c.MachineName.Contains(request.Name) || request.Ids.Contains(c.MachineId));
                }

                if (string.IsNullOrWhiteSpace(request.Prop) == false)
                {
                    if (request.Asc)
                    {
                        list = request.Prop switch
                        {
                            "MachineId" => list.OrderBy(c => c.MachineName),
                            "Version" => list.OrderBy(c => c.Version),
                            _ => list.OrderBy(c => c.Order)
                        };
                    }
                    else
                    {
                        list = request.Prop switch
                        {
                            "MachineId" => list.OrderByDescending(c => c.MachineName),
                            "Version" => list.OrderByDescending(c => c.Version),
                            _ => list.OrderByDescending(c => c.Order)
                        };
                    }
                }

                int count = list.Count();
                list = list.Skip((request.Page - 1) * request.Size).Take(request.Size);

                List<SignCacheInfo> result = [cache, .. list];
                result = result.Select(c => new SignCacheInfo
                {
                    Connected = c.Connected,
                    GroupId = c.GroupId,
                    IP = c.IP,
                    LastSignIn = c.LastSignIn,
                    MachineId = c.MachineId,
                    MachineName = c.MachineName,
                    Version = c.Version,
                }).ToList();


                SignInListResponseInfo response = new SignInListResponseInfo { Request = request, Count = count, List = result };

                connection.Write(MemoryPackSerializer.Serialize(response));
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Delete)]
        public void Delete(IConnection connection)
        {
            string name = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(name, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                signCaching.TryRemove(name, out _);
            }
        }

        [MessengerId((ushort)SignInMessengerIds.SetNameForward)]
        public async Task NameForward(IConnection connection)
        {
            ConfigSetNameInfo info = MemoryPackSerializer.Deserialize<ConfigSetNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(info.Id, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                if (info.Id != connection.Id)
                {
                    await messengerSender.SendOnly(new MessageRequestWrap
                    {
                        Connection = cache.Connection,
                        MessengerId = (ushort)SignInMessengerIds.SetName,
                        Payload = connection.ReceiveRequestWrap.Payload,
                    }).ConfigureAwait(false);
                }
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Version)]
        public void Version(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(config.Data.Version));
        }


        [MessengerId((ushort)SignInMessengerIds.Ids)]
        public void Ids(IConnection connection)
        {
            SignInIdsRequestInfo request = MemoryPackSerializer.Deserialize<SignInIdsRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                IEnumerable<SignCacheInfo> list = signCaching.Get(cache.GroupId).OrderByDescending(c => c.MachineName).OrderByDescending(c => c.LastSignIn).OrderByDescending(c => c.Version).ToList();
                if (string.IsNullOrWhiteSpace(request.Name) == false)
                {
                    list = list.Where(c => c.MachineName.Contains(request.Name));
                }
                int count = list.Count();
                list = list.Skip((request.Page - 1) * request.Size).Take(request.Size);

                SignInIdsResponseInfo response = new SignInIdsResponseInfo
                {
                    Request = request,
                    Count = count,
                    List = list.Select(c => new SignInIdsResponseItemInfo { MachineId = c.MachineId, MachineName = c.MachineName }).ToList()
                };

                connection.Write(MemoryPackSerializer.Serialize(response));
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Exists)]
        public void Exists(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                IEnumerable<string> list = signCaching.Get(cache.GroupId).Select(c => c.MachineId);
                connection.Write(MemoryPackSerializer.Serialize(list));
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Online)]
        public void Online(IConnection connection)
        {
            string machineId = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);

            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache) && signCaching.TryGet(machineId, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId && cache1.Connected)
            {
                connection.Write(Helper.TrueArray);
            }
            else
            {
                connection.Write(Helper.FalseArray);
            }
        }

        [MessengerId((ushort)SignInMessengerIds.NewId)]
        public void NewId(IConnection connection)
        {
            connection.Write(MemoryPackSerializer.Serialize(ObjectId.NewObjectId().ToString()));
        }
    }

    [MemoryPackable]
    public sealed partial class SignInListRequestInfo
    {
        /// <summary>
        /// 当前页
        /// </summary>
        public int Page { get; set; } = 1;
        /// <summary>
        /// 每页大小
        /// </summary>
        public int Size { get; set; } = 10;
        /// <summary>
        /// 按名称搜索
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 按id获取
        /// </summary>
        public string[] Ids { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public bool Asc { get; set; }
        /// <summary>
        /// 排序字段
        /// </summary>
        public string Prop { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SignInListResponseInfo
    {
        public SignInListRequestInfo Request { get; set; } = new SignInListRequestInfo();
        public int Count { get; set; }
        public List<SignCacheInfo> List { get; set; } = new List<SignCacheInfo>();
    }


    [MemoryPackable]
    public sealed partial class SignInIdsRequestInfo
    {
        /// <summary>
        /// 当前页
        /// </summary>
        public int Page { get; set; } = 1;
        /// <summary>
        /// 每页大小
        /// </summary>
        public int Size { get; set; } = 10;
        /// <summary>
        /// 按名称搜索
        /// </summary>
        public string Name { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SignInIdsResponseInfo
    {
        public SignInIdsRequestInfo Request { get; set; } = new SignInIdsRequestInfo();
        public int Count { get; set; }
        public List<SignInIdsResponseItemInfo> List { get; set; } = new List<SignInIdsResponseItemInfo>();
    }

    [MemoryPackable]
    public sealed partial class SignInIdsResponseItemInfo
    {
        public string MachineId { get; set; }
        public string MachineName { get; set; }
    }

    [MemoryPackable]
    public sealed partial class SignInResponseInfo
    {
        public bool Status { get; set; }
        public string MachineId { get; set; }
        public string Msg { get; set; }
    }
}
