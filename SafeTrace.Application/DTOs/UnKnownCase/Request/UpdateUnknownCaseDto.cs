using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.UnKnownCase.Request
{
    public class UpdateUnknownCaseDto : CaseUpdateBaseDto
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string? FName { get; set; }
        
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string? LName { get; set; }
    }
}