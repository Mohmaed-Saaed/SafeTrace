using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Response
{
    public sealed class PublishFacebookImportedPostResponseDto
    {
        public long FacebookImportedPostId { get; set; }
        public FacebookImportedPostStatus Status { get; set; }

        // When successfully published as a new Case:
        public long? CaseId { get; set; }
        public string? CaseCode { get; set; }
        public CaseType? CaseType { get; set; }

        // When an existing duplicate Case is detected:
        public long? ExistingCaseId { get; set; }
        public string? ExistingCaseCode { get; set; }
        public CaseType? ExistingCaseType { get; set; }
        public double? MatchConfidence { get; set; }
        public string? Message { get; set; }
    }
}
