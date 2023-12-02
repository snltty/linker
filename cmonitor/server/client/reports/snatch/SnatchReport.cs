namespace cmonitor.server.client.reports.snatch
{
    public sealed class SnatchReport : IReport
    {
        public string Name => "Snatch";

        private readonly ISnatch snatch;

        public SnatchReport(ISnatch snatch)
        {
            this.snatch = snatch;
            snatch.Set(new SnatchQuestionInfo { Correct = string.Empty, End = true, Join=0, Max=0, Option=0, Question=string.Empty, Repeat=false, Right=0, Type= SnatchType.Select,Wrong=0});
        }

        public object GetReports(ReportType reportType)
        {
            return null;
        }

        public void Update(SnatchQuestionInfo snatchQuestionInfo)
        {
            snatch.Set(snatchQuestionInfo);
        }
    }
}
