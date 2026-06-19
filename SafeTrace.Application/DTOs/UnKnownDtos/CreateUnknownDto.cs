
using Microsoft.AspNetCore.Http;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.UnKnownDtos
{
    public class CreateUnknownDto : IValidatableObject
    {
        public Gender Gender { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }

        [Required]
        public string Government { get; set; } = null!;

        [Required]
        public string City { get; set; } = null!;

        [Required]
        public string Street { get; set; } = null!;

        public string? CommunicationPhone { get; set; }
        public string? Description { get; set; }

        public List<IFormFile> Photos { get; set; } = new();
        public int AgeCategoryId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Photos == null || Photos.Count == 0)
            {
                yield return new ValidationResult(
                    "You must upload at least one photo.",
                    new[] { nameof(Photos) });
            }
        }

    }
}
