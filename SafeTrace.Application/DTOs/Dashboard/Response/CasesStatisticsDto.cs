namespace SafeTrace.Application.DTOs.Dashboard.Response
{
    public class CasesStatisticsDto
    {
        public int Total { get; set; }
        public int Urgent { get; set; }
        public int LongTerm { get; set; }
        public int Unknown { get; set; }
        public int Active { get; set; }
        public int Found { get; set; }
    }
}
