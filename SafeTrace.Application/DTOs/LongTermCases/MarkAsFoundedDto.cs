using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.DTOs.LongTermCases
{
    /// <summary>
    /// Payload for the "Mark as Founded" confirmation popup (FR-47 .. FR-50).
    /// A recovery story (Description) is mandatory.
    /// </summary>
    public class MarkAsFoundedDto
    {
        /// <summary>Recovery story / how & where the person was found.</summary>
        [Required, MaxLength(2000)]
        public string Description { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Government { get; set; } = null!;

        [Required, MaxLength(200)]
        public string City { get; set; } = null!;

        [MaxLength(500)]
        public string? Street { get; set; }
    }
}
