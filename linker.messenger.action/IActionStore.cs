namespace linker.messenger.action
{
    public sealed class ActionInfo
    {
        public string Arg { get; set; } = string.Empty;
        public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();
    }
    public interface IActionStore
    {
        public string SignInActionUrl{ get; }
        public string RelayActionUrl { get; }
        public string SForwardActionUrl { get; }
        public void SetActionArg(string action);
        public void SetActionArgs(Dictionary<string, string> actions);
        public bool TryAddActionArg(string host, Dictionary<string, string> args);
        public bool TryGetActionArg(Dictionary<string, string> args, out string str, out string machineKey);
    }
}
