using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.UnKnownCase.Request
{
    public class UpdateUnknownCaseDto
    {
        public Gender Gender { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }
        public int Age { get; set; }

        
        public string Government { get; set; } 

        public string City { get; set; } 

        public string Street { get; set; }

        public string CommunicationPhone { get; set; }
        public string Description { get; set; }
        public List<IFormFile> NewPhotos { get; set; } = new();

        public List<long> DeletedPhotoIds { get; set; } = new();

    }
}
