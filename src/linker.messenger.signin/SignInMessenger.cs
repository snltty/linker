using linker.libs;
using linker.libs.extends;
using System.Diagnostics;
using System.Net;

namespace linker.messenger.signin
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SignInClientMessenger : IMessenger
    {
        private readonly ISignInClientStore signInClientStore;
        private readonly SignInClientTransfer signInClientTransfer;
        private readonly ISerializer serializer;
        public SignInClientMessenger(ISignInClientStore signInClientStore, SignInClientTransfer signInClientTransfer, ISerializer serializer)
        {
            this.signInClientStore = signInClientStore;
            this.signInClientTransfer = signInClientTransfer;
            this.serializer = serializer;
        }

        [MessengerId((ushort)SignInMessengerIds.SetName)]
        public void Name(IConnection connection)
        {
            SignInConfigSetNameInfo info = serializer.Deserialize<SignInConfigSetNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
            signInClientStore.SetName(info.NewName);
            signInClientTransfer.ReSignIn();
        }

    }
    /// <summary>
    /// 登录的服务端信标
    /// </summary>
    public class SignInServerMessenger : IMessenger
    {
        private readonly SignInServerCaching signCaching;
        private readonly IMessengerSender messengerSender;
        private readonly ISerializer serializer;

        public SignInServerMessenger(IMessengerSender messengerSender, SignInServerCaching signCaching, ISerializer serializer)
        {
            this.signCaching = signCaching;
            this.messengerSender = messengerSender;
            this.serializer = serializer;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            connection.Disponse();
            return;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn_V_1_3_1)]
        public async Task SignIn_V_1_3_1(IConnection connection)
        {
            var sw = new Stopwatch();
            sw.Start();

            SignInfo info = serializer.Deserialize<SignInfo>(connection.ReceiveRequestWrap.Payload.Span);

            LoggerHelper.Instance.Info($"sign in from >=v131 {connection.Address}->{info.ToJson()}");

            info.Connection = connection;
            string msg = await signCaching.Sign(info).ConfigureAwait(false);

            SignInResponseInfo resp = new SignInResponseInfo
            {
                Status = string.IsNullOrWhiteSpace(msg),
                MachineId = info.MachineId,
                Msg = msg,
                IP = connection.Address
            };

            sw.Stop();

            connection.Write(serializer.Serialize(resp.ToJson()));
        }


        [MessengerId((ushort)SignInMessengerIds.SetOrder)]
        public void SetOrder(IConnection connection)
        {
            string[] ids = serializer.Deserialize<string[]>(connection.ReceiveRequestWrap.Payload.Span);
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
            SignInListRequestInfo request = serializer.Deserialize<SignInListRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
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

                connection.Write(serializer.Serialize(response));
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Delete)]
        public void Delete(IConnection connection)
        {
            string name = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(name, out SignCacheInfo cache) && signCaching.TryGet(connection.Id, out SignCacheInfo cache1) && cache.GroupId == cache1.GroupId)
            {
                signCaching.TryRemove(name, out _);
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Version)]
        public void Version(IConnection connection)
        {
            connection.Write(serializer.Serialize(VersionHelper.version));
        }


        [MessengerId((ushort)SignInMessengerIds.Ids)]
        public void Ids(IConnection connection)
        {
            SignInIdsRequestInfo request = serializer.Deserialize<SignInIdsRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
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

                connection.Write(serializer.Serialize(response));
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Names)]
        public void Names(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                List<SignInNamesResponseItemInfo> list = signCaching.Get(cache.GroupId).Select(c => new SignInNamesResponseItemInfo { MachineId = c.MachineId, MachineName = c.MachineName, Online = c.Connected }).ToList();

                connection.Write(serializer.Serialize(list));
            }
        }


        [MessengerId((ushort)SignInMessengerIds.Exists)]
        public void Exists(IConnection connection)
        {
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                IEnumerable<string> list = signCaching.Get(cache.GroupId).Select(c => c.MachineId);
                connection.Write(serializer.Serialize(list));
            }
        }

        [MessengerId((ushort)SignInMessengerIds.Online)]
        public void Online(IConnection connection)
        {
            string machineId = serializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);

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
            connection.Write(serializer.Serialize(signCaching.NewId()));
        }

        [MessengerId((ushort)SignInMessengerIds.SetNameForward)]
        public async Task SetNameForward(IConnection connection)
        {
            SignInConfigSetNameInfo info = serializer.Deserialize<SignInConfigSetNameInfo>(connection.ReceiveRequestWrap.Payload.Span);
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

        [MessengerId((ushort)SignInMessengerIds.PushArg)]
        public void PushArg(IConnection connection)
        {
            SignInPushArgInfo info = serializer.Deserialize<SignInPushArgInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                cache.Args.TryAdd(info.Key, info.Value);
            }
        }
    }

    /// <summary>
    /// 客户端列表查询
    /// </summary>
    public sealed class SignInListRequestInfo
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
    /// <summary>
    /// 客户端列表查询返回
    /// </summary>
    public sealed class SignInListResponseInfo
    {
        public SignInListRequestInfo Request { get; set; } = new SignInListRequestInfo();
        public int Count { get; set; }
        public List<SignCacheInfo> List { get; set; } = new List<SignCacheInfo>();
    }
    /// <summary>
    /// 查询客户端id列表
    /// </summary>
    public sealed class SignInIdsRequestInfo
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
    /// <summary>
    /// 查询客户端id列表 返回
    /// </summary>
    public sealed class SignInIdsResponseInfo
    {
        public SignInIdsRequestInfo Request { get; set; } = new SignInIdsRequestInfo();
        public int Count { get; set; }
        public List<SignInIdsResponseItemInfo> List { get; set; } = new List<SignInIdsResponseItemInfo>();
    }

    public sealed class SignInIdsResponseItemInfo
    {
        public string MachineId { get; set; }
        public string MachineName { get; set; }
    }

    public sealed class SignInNamesResponseItemInfo
    {
        public string MachineId { get; set; }
        public string MachineName { get; set; }
        public bool Online { get; set; }
    }

    /// <summary>
    /// 登录返回
    /// </summary>
    public sealed class SignInResponseInfo
    {
        public bool Status { get; set; }
        public string MachineId { get; set; }
        public IPEndPoint IP { get; set; }
        public string Msg { get; set; }
    }


    public sealed class SignInPushArgInfo
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
