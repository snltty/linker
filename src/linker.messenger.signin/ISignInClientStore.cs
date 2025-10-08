namespace linker.messenger.signin
{
    public interface ISignInClientStore
    {
        /// <summary>
        /// 信标服务器
        /// </summary>
        public SignInClientServerInfo Server { get; }
        /// <summary>
        /// 分组
        /// </summary>
        public SignInClientGroupInfo Group { get; }
        public SignInClientGroupInfo[] Groups { get; }

        /// <summary>
        /// id
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; }
        public string[] Hosts { get; }

        /// <summary>
        /// 设置名称
        /// </summary>
        /// <param name="newName"></param>
        public void SetName(string newName);
        /// <summary>
        /// 设置分组，第一个生效
        /// </summary>
        /// <param name="groups"></param>
        public void SetGroups(SignInClientGroupInfo[] groups);
        /// <summary>
        /// 设置生效分组的密码
        /// </summary>
        /// <param name="password"></param>
        public void SetGroupPassword(string password);
        /// <summary>
        /// 设置信标服务器
        /// </summary>
        /// <param name="servers"></param>
        public void SetServer(SignInClientServerInfo servers);
        /// <summary>
        /// 设置信标密钥
        /// </summary>
        /// <param name="key"></param>
        /// <param name="password"></param>
        public void SetSuper(string key,string password);
        /// <summary>
        /// 设置用户id
        /// </summary>
        /// <param name="userid"></param>
        public void SetUserId(string userid);
        /// <summary>
        /// 信标服务器
        /// </summary>
        /// <param name="host"></param>
        /// <param name="host1"></param>
        public void SetHost(string host, string host1);
        public void SetHosts(string[] hosts);
        /// <summary>
        /// 设置id
        /// </summary>
        /// <param name="id"></param>
        public void SetId(string id);

        public bool Confirm();
    }

}
