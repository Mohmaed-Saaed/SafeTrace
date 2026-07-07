using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.DTOs.LongTermCase.Request
{
    public class UpdateLongTermCaseDto : CaseUpdateBaseDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string FName { get; set; } = null!;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string LName { get; set; } = null!;

        [EnumDataType(typeof(RelationType), ErrorMessage = "Invalid relation type.")]
        public RelationType? Relation { get; set; }

        /// <summary>If provided, replaces the existing police report image.</summary>
        [MaxPhotoSize(10, ErrorMessage = "Police report must not exceed 10 MB.")]
        [AllowedPhotoTypes(ErrorMessage = "Only JPEG, PNG, and WebP images are allowed.")]
        public IFormFile? PoliceReportImage { get; set; }
    }
}