
using Microsoft.AspNetCore.Http;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.DTOs
{
    public class CreateUnknownDto
    {
        public Gender Gender { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }

        public string Government { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Street { get; set; } = null!;
        public string? CommunicationPhone { get; set; }
        public string? Description { get; set; }

        public List<IFormFile> Photos { get; set; } = new();
        public int AgeCategoryId { get; set; }

    }
}
