using linker.libs;
using linker.messenger.signin;

namespace linker.plugins.signIn.args
{
    /// <summary>
    /// 给登录加一个唯一ID的参数
    /// </summary>
    public sealed class SignInArgsMachineKeyClient : ISignInArgs
    {
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            string machineKey = SystemIdHelper.GetSystemId();
            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Debug($"machine key :{machineKey}");
            if (string.IsNullOrWhiteSpace(machineKey))
            {
                return $"get machine key fail";
            }

            args.TryAdd("machineKey", machineKey);

            await Task.CompletedTask;

            return string.Empty;
        }

        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            await Task.CompletedTask;
            return string.Empty;
        }
    }

    /// <summary>
    /// 验证登录唯一参数
    /// </summary>
    public sealed class SignInArgsMachineKeyServer : ISignInArgs
    {
        public async Task<string> Invoke(string host, Dictionary<string, string> args)
        {
            await Task.CompletedTask;
            return string.Empty;
        }

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <param name="signInfo">新登录参数</param>
        /// <param name="cache">之前的登录信息</param>
        /// <returns></returns>
        public async Task<string> Validate(SignInfo signInfo, SignCacheInfo cache)
        {
            //放宽条件，只有已经登录时不能再次登录
            if (cache != null && cache.Connected)
            {
                signInfo.Args.TryGetValue("machineKey", out string keyNew);
                cache.Args.TryGetValue("machineKey", out string keyOld);

                //之前的登录有唯一编号的，则验证，唯一编号不一样，不允许登录
                if (string.IsNullOrWhiteSpace(keyOld) == false && keyNew != keyOld)
                {
                    return $"your id 【{signInfo.MachineId}】 is already online, online machineName {cache.MachineName}";
                }
            }
            await Task.CompletedTask;
            return string.Empty;
        }


    }
}
