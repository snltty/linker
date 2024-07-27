using linker.config;
using MemoryPack;
using linker.plugins.client;
using linker.plugins.server;
using linker.plugins.messenger;

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
            clientSignInTransfer.SetName(info.NewName);
        }

    }

    public sealed class SignInServerMessenger : IMessenger
    {
        private readonly SignCaching signCaching;
        private readonly FileConfig config;
        private readonly MessengerSender messengerSender;

        public SignInServerMessenger(SignCaching signCaching, FileConfig config, MessengerSender messengerSender)
        {
            this.signCaching = signCaching;
            this.config = config;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)SignInMessengerIds.SignIn)]
        public void SignIn(IConnection connection)
        {
            SignInfo info = MemoryPackSerializer.Deserialize<SignInfo>(connection.ReceiveRequestWrap.Payload.Span);
            signCaching.Sign(connection, info);
            connection.Write(MemoryPackSerializer.Serialize(info.MachineId));
        }


        [MessengerId((ushort)SignInMessengerIds.List)]
        public void List(IConnection connection)
        {
            SignInListRequestInfo request = MemoryPackSerializer.Deserialize<SignInListRequestInfo>(connection.ReceiveRequestWrap.Payload.Span);
            if (signCaching.TryGet(connection.Id, out SignCacheInfo cache))
            {
                IEnumerable<SignCacheInfo> list = signCaching.Get(cache.GroupId).OrderByDescending(c => c.MachineName).OrderByDescending(c => c.LastSignIn).OrderByDescending(c => c.Version).ToList();
                if (string.IsNullOrWhiteSpace(request.Name) == false)
                {
                    list = list.Where(c => c.Version.Contains(request.Name) || c.IP.ToString().Contains(request.Name) || c.MachineName.Contains(request.Name) || request.Ids.Contains(c.MachineId));
                }
                int count = list.Count();
                list = list.Skip((request.Page - 1) * request.Size).Take(request.Size);

                SignInListResponseInfo response = new SignInListResponseInfo { Request = request, Count = count, List = list.ToList() };

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
        /// 所在分组
        /// </summary>
        public string GroupId { get; set; }
        /// <summary>
        /// 按名称搜索
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 按id获取
        /// </summary>
        public string[] Ids { get; set; }
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
}
