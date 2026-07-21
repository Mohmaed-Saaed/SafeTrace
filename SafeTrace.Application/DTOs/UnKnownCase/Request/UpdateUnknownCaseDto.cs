using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.UnKnownCase.Request
{
    public class UpdateUnknownCaseDto : CaseUpdateBaseDto
    {
        [ArabicText(ErrorMessage = "First name must contain Arabic letters and spaces only.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 60 characters.")]
        public string? FName { get; set; }
        
        [ArabicText(ErrorMessage = "Last name must contain Arabic letters and spaces only.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 60 characters.")]
        public string? LName { get; set; }
    }
}
