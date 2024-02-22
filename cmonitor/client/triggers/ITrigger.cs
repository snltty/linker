namespace cmonitor.client.triggers
{
    public interface ITrigger
    {
        public string Name { get; }
        public string Desc { get; }
        public void Execute();
    }
}
