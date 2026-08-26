using SafeTrace.Application.Common.Enums;

namespace SafeTrace.Application.DTOs.AiCaseAnalyzer.Response
{
    public sealed class SocialPostAiResultDto
    {
        public SocialPostClassification Classification { get; set; }

        public double Confidence { get; set; }

        public PersonDto Person { get; set; } = new();

        public MissingInfoDto MissingInfo { get; set; } = new();

        public ContactDto Contact { get; set; } = new();

        public string? Description { get; set; }

        public List<string> Warnings { get; set; } = [];

        public sealed class PersonDto
        {
            public string? FirstName { get; set; }

            public string? SecondName { get; set; }

            public string? ThirdName { get; set; }

            public string? LastName { get; set; }

            public int? Age { get; set; }

            public Gender? Gender { get; set; }
        }

        public sealed class MissingInfoDto
        {
            public DateOnly? EventDate { get; set; }

            public string? Government { get; set; }

            public string? City { get; set; }

            public string? Street { get; set; }

            public string? LastSeenLocation { get; set; }
        }

        public sealed class ContactDto
        {
            public string? Phone { get; set; }
        }
    }
}