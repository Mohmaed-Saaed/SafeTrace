using SafeTrace.Domain.Enums;

namespace SafeTrace.Domain.Entities
{
    public class Complaint
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public string? CaseCode { get; set; }
        public string Message { get; set; } = null!;
        public string? SolutionMessage { get; set; }

        public ComplaintStatus ComplaintStatus { get; set; } = ComplaintStatus.UnSolved;
        public DateTime CreatedAt { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
