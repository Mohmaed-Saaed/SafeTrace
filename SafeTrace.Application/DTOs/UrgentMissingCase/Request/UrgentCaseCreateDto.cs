using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.UrgentMissingCase.Request
{
    public class UrgentCaseCreateDto
    {
        public Gender Gender { get; set; }
        public string? Government { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }
        public string? CommunicationPhone { get; set; }
        public RelationType Relation { get; set; }
        public DateTime EventDate { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<IFormFile>? Photos { get; set; }
    }
}


