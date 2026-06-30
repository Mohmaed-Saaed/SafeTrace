using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.UnKnownDtos
{
    public class UnknownPhotoDto
    {
        public int PhotoId { get; set; }
        public string ImagePath { get; set; } = null!;
        public bool IsPrimary { get; set; }
    }
}
