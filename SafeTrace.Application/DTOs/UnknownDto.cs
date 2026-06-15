
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs
{
    public class UnknownDto
    {
        public Gender Gender { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }

        public string? CommunicationPhone { get; set; }

        public RelationType Relation { get; set; }

        public string? Description { get; set; }

        public int AgeCategoryId { get; set; }

    }
}
