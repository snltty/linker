namespace cmonitor.client.report
{
    public abstract class ReportInfo
    {
        public abstract int HashCode();

        private int hashcode = 0;
        public bool Updated()
        {
            int code = HashCode();
            bool updated = code != hashcode;
            hashcode = code;
            return updated;
        }
    }
}
