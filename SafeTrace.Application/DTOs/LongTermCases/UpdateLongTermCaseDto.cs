using Microsoft.AspNetCore.Http;
using SafeTrace.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.DTOs.LongTermCases
{
    /// <summary>
    /// Payload for updating a Long-Term Missing Case. Only the case owner (or an Admin)
    /// can update it. All properties are optional - only the supplied ones are changed.
    /// </summary>
    public class UpdateLongTermCaseDto
    {
        public Gender? Gender { get; set; }
        public string? FName { get; set; }
        public string? SName { get; set; }
        public string? TName { get; set; }
        public string? LName { get; set; }

        [Range(0, 150)]
        public int? Age { get; set; }

        public RelationType? Relation { get; set; }



        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Government { get; set; }

        [MaxLength(200)]
        public string? City { get; set; }

        [MaxLength(500)]
        public string? Street { get; set; }

        /// <summary>If provided, replaces the existing police report image.</summary>
        public IFormFile? PoliceReportImage { get; set; }

        /// <summary>New photos to append to the case (existing photos are kept unless removed below).</summary>
        public List<IFormFile>? NewPhotos { get; set; }

        /// <summary>Ids of existing CasePhoto records to delete.</summary>
        public List<long>? RemovedPhotoIds { get; set; }
    }
}
