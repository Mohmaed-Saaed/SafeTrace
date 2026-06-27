using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.AiMatching.Response
{
    public class MatchedCaseDto
    {
        public long Id { get; set; }
        public Gender Gender { get; set; }
        public string Government { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public int Age { get; set; }
        public string UserId { get; set; } = null!;
        public string? CommunicationPhone { get; set; }
        public string CaseCode { get; set; } = null!;
        public DateTime EventDate { get; set; }
        public string? Description { get; set; }
        public CaseType CaseType { get; set; }
        public float Similarity { get; set; }
        public string? MainPhotoPath { get; set; }
    }
}