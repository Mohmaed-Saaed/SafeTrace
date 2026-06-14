namespace SafeTrace.Domain.Entities
{
        public class LongTermMissingCase : BaseCase
        {
            public string Government { get; set; } = null!;

            public string City { get; set; } = null!;

            public string Street { get; set; } = null!;

            public string? PoliceReportImage { get; set; }
        }
}
