namespace linker.messenger.relay.server
{
    public sealed class RelayServerNodeReportTransfer
    {
        private uint connectionNum = 0;
        private long bytes = 0;

        public uint ConnectionNum => connectionNum;

        /// <summary>
        /// 增加连接数
        /// </summary>
        public void IncrementConnectionNum()
        {
            Interlocked.Increment(ref connectionNum);
        }
        /// <summary>
        /// 减少连接数
        /// </summary>
        public void DecrementConnectionNum()
        {
            Interlocked.Decrement(ref connectionNum);
        }

        public void AddBytes(long length)
        {
            Interlocked.Add(ref bytes, length);
        }
    }
}
