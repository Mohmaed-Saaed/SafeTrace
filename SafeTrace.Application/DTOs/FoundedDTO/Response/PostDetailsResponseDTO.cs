using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.FoundedDTO.Response
{
    public class PostDetailsResponseDTO
    {
        public string FullName { get; set; } = null!;
        public string MainImage { get; set; } = null!;
        public string Age { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public string FoundDescription { get; set; } = null!;
        public string MissingDescription { get; set; } = null!;
        public string FoundLocation { get; set; } = null!;
        public DateOnly FounedDate { get; set; } = default!;
        public string MissingLocation { get; set; } = null!;
    }
}
