using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.LongTermCase.Request
{
    /// <summary>
    /// Payload for creating a new Long-Term Missing Case (FR-18 .. FR-20).
    /// Only Verified Users / Admins are allowed to submit this (enforced via [Authorize] on the controller).
    /// The new case is created with Status = Pending and requires Admin approval before becoming public.
    /// </summary>
    public class CreateLongTermCaseDto
    {
        [Required]
        public Gender Gender { get; set; }
        
        [Required, MaxLength(100)]
        public string FName { get; set; } = null!;
        public string? SName { get; set; }
        public string? TName { get; set; }

        [Required, MaxLength(100)]
        public string LName { get; set; } = null!;

        [Range(0, 150)]
        public int Age { get; set; }

        [Required]
        public RelationType Relation { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        // Last known location of the missing person (LongTermMissingCaseConfiguration)
        [Required, MaxLength(200)]
        public string Government { get; set; } = null!;

        [Required, MaxLength(200)]
        public string City { get; set; } = null!;

        [MaxLength(500)]
        public string? Street { get; set; }

        /// <summary>Optional scanned police report.</summary>
        public IFormFile? PoliceReportImage { get; set; }

        /// <summary>Photos of the missing person.</summary>
        public List<IFormFile>? Photos { get; set; }
        public int  PrimaryPhotoIndex { get; set; }
    }
}
