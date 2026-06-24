using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.UrgentMissingCase.Request
{
    public class UrgentCaseCreateDto
    {
        [Required]
        public string UserId { get; set; } = null!;
        public Gender Gender { get; set; }
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }
        public string? CommunicationPhone { get; set; }
        public RelationType Relation { get; set; }
        public DateTime EventDate { get; set; }
        public string? Description { get; set; }
        [Required]
        public double Latitude { get; set; }
        [Required]
        public double Longitude { get; set; }
        [Required]
        public List<IFormFile>? Photos { get; set; }
    }
}


