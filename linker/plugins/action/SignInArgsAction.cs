using linker.config;
using linker.libs.extends;
using linker.messenger.signin;
using linker.plugins.sforward.config;
using linker.plugins.sforward.validator;
using linker.messenger.relay.server.validator;
using System.Net;
using System.Text.Json.Nodes;

namespace linker.plugins.action
{
    public sealed class JsonArgInfo
    {
        /// <summary>
        /// 登入信息，每次都会携带
        /// </summary>
        public JsonArgSignInInfo Signin { get; set; }
        /// <summary>
        /// 中继信息，非中继验证时为null
        /// </summary>
        public JsonArgRelayInfo Relay { get; set; }
        /// <summary>
        /// 穿透信息，非穿透验证时为null
        /// </summary>
        public JsonArgSForwardInfo SForward { get; set; }
    }
    public sealed class JsonArgSignInInfo
    {
        /// <summary>
        /// 设备id
        /// </summary>
        public string MachineId { get; set; } = string.Empty;
        /// <summary>
        /// 设备名
        /// </summary>
        public string MachineName { get; set; } = string.Empty;
        /// <summary>
        /// 设备所在机器的编号
        /// </summary>
        public string MachineKey { get; set; } = string.Empty;
        /// <summary>
        /// IP地址
        /// </summary>
        public IPAddress IPAddress { get; set; } = IPAddress.Any;
        /// <summary>
        /// 分组id
        /// </summary>
        public string GroupId { get; set; } = string.Empty;
    }
    public sealed class JsonArgRelayInfo
    {
        /// <summary>
        /// 来源设备id
        /// </summary>
        public string FromMachineId { get; set; }
        /// <summary>
        /// 来源设备名
        /// </summary>
        public string FromMachineName { get; set; }
        /// <summary>
        /// 目标设备id
        /// </summary>
        public string RemoteMachineId { get; set; }
        /// <summary>
        /// 目标设备名
        /// </summary>
        public string RemoteMachineName { get; set; }
        /// <summary>
        /// 事务id
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// 协议名
        /// </summary>
        public string TransportName { get; set; }
        /// <summary>
        /// 流水id
        /// </summary>
        public ulong FlowingId { get; set; }
    }
    public sealed class JsonArgSForwardInfo
    {
        /// <summary>
        /// 域名
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 端口
        /// </summary>
        public int RemotePort { get; set; }
    }


    public class JsonArgReplace
    {
        public string Replace(JsonArgInfo info, string jsonstr)
        {
            JsonNode json = JsonObject.Parse(jsonstr);
            json["JsonArg"] = JsonNode.Parse(info.ToJson());
            return json.ToJson();
        }
    }

    public sealed class SignInArgsAction : JsonArgReplace, ISignInArgs
    {
        private readonly ActionTransfer actionTransfer;
        private readonly FileConfig fileConfig;

        public SignInArgsAction(ActionTransfer actionTransfer, FileConfig fileConfig)
        {
            this.actionTransfer = actionTransfer;
            this.fileConfig = fileConfig;
        }

        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            actionTransfer.TryAddActionArg(host, args);
            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            if (string.IsNullOrWhiteSpace(actionTransfer.SignInActionUrl) == false)
            {
                if (actionTransfer.TryGetActionArg(signInfo.Args, out string str, out string machineKey) == false)
                {
                    return $"singin action URL exists, but [{signInfo.MachineName}] action value is not configured";
                }

                JsonArgInfo replace = new JsonArgInfo
                {
                    Signin = new JsonArgSignInInfo
                    {
                        GroupId = signInfo.GroupId,
                        MachineId = signInfo.MachineId,
                        MachineName = signInfo.MachineName,
                        MachineKey = machineKey,
                        IPAddress = signInfo.Connection.Address.Address
                    }
                };
                return await actionTransfer.ExcuteActions(Replace(replace, str), actionTransfer.SignInActionUrl);
            }

            return string.Empty;
        }
    }

    public sealed class RelayValidatorAction : JsonArgReplace, IRelayServerValidator
    {
        private readonly ActionTransfer actionTransfer;
        private readonly FileConfig fileConfig;

        public RelayValidatorAction(ActionTransfer actionTransfer, FileConfig fileConfig)
        {
            this.actionTransfer = actionTransfer;
            this.fileConfig = fileConfig;
        }

        public async Task<string> Validate(linker.messenger.relay.client.transport.RelayInfo relayInfo, SignCacheInfo fromMachine, SignCacheInfo toMachine)
        {
            if (string.IsNullOrWhiteSpace(actionTransfer.RelayActionUrl) == false)
            {
                if (actionTransfer.TryGetActionArg(fromMachine.Args, out string str, out string machineKey) == false)
                {
                    return $"relay action URL exists, but [{fromMachine.MachineName}] action value is not configured";
                }
                if (toMachine != null && actionTransfer.TryGetActionArg(toMachine.Args, out string str1, out string machineKey1) == false)
                {
                    return $"relay action URL exists, but [{toMachine.MachineName}]e action value is not configured";
                }
                JsonArgInfo replace = new JsonArgInfo
                {
                    Relay = new JsonArgRelayInfo
                    {
                        FromMachineId = relayInfo.FromMachineId ?? string.Empty,
                        FromMachineName = relayInfo.FromMachineName ?? string.Empty,
                        RemoteMachineId = relayInfo.RemoteMachineId ?? string.Empty,
                        RemoteMachineName = relayInfo.RemoteMachineName ?? string.Empty,
                        TransactionId = relayInfo.TransactionId ?? string.Empty,
                        TransportName = relayInfo.TransportName ?? string.Empty,
                        FlowingId = relayInfo.FlowingId,
                    },
                    Signin = new JsonArgSignInInfo
                    {
                        GroupId = fromMachine.GroupId,
                        MachineId = fromMachine.MachineId,
                        MachineName = fromMachine.MachineName,
                        MachineKey = machineKey,
                        IPAddress = fromMachine.Connection.Address.Address,
                    }
                };
                return await actionTransfer.ExcuteActions(Replace(replace, str), actionTransfer.RelayActionUrl);
            }
            return string.Empty;
        }
    }

    public sealed class SForwardValidatorAction : JsonArgReplace, ISForwardValidator
    {
        private readonly ActionTransfer actionTransfer;
        private readonly FileConfig fileConfig;

        public SForwardValidatorAction(ActionTransfer actionTransfer, FileConfig fileConfig)
        {
            this.actionTransfer = actionTransfer;
            this.fileConfig = fileConfig;
        }

        public async Task<string> Validate(SignCacheInfo cache, SForwardAddInfo sForwardAddInfo)
        {
            if (string.IsNullOrWhiteSpace(actionTransfer.SForwardActionUrl) == false)
            {
                if (actionTransfer.TryGetActionArg(cache.Args, out string str, out string machineKey) == false)
                {
                    return $"sforward action URL exists, but [{cache.MachineName}] action value is not configured";
                }

                JsonArgInfo replace = new JsonArgInfo
                {
                    SForward = new JsonArgSForwardInfo
                    {
                        Domain = sForwardAddInfo.Domain ?? string.Empty,
                        RemotePort = sForwardAddInfo.RemotePort
                    },
                    Signin = new JsonArgSignInInfo
                    {
                        GroupId = cache.GroupId,
                        MachineId = cache.MachineId,
                        MachineName = cache.MachineName,
                        MachineKey = machineKey,
                        IPAddress = cache.Connection.Address.Address
                    }
                };
                return await actionTransfer.ExcuteActions(Replace(replace, str), actionTransfer.SForwardActionUrl);
            }
            return string.Empty;
        }
    }
}
