using Microsoft.AspNetCore.Http;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SafeTrace.Application.DTOs.UnKnownDtos
{
    public class UpdateUnkownCaseDto
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

        public string CommunicationPhone { get; set; }
        public string Description { get; set; }
        public List<IFormFile> NewPhotos { get; set; } = new();

        public List<long> DeletedPhotoIds { get; set; } = new();
        public int AgeCategoryId { get; set; }
    }
}
