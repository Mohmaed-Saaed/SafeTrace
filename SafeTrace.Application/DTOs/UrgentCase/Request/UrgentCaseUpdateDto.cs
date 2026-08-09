using Microsoft.AspNetCore.Http;
using SafeTrace.Application.Common.Validators.Attributes;
using SafeTrace.Application.DTOs.Cases.Request;
using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.UrgentCase.Request
{
    public class UrgentCaseUpdateDto : CaseUpdateBaseDto
    {
        [Required(ErrorMessage = "Event date is required.")]
        [UrgentEventDate(6)]
        [DataType(DataType.Date)]
        public DateTime? EventDate { get; set; }

        [EnumDataType(typeof(RelationType), ErrorMessage = "Invalid relation type.")]
        public RelationType? Relation { get; set; }

        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }
    }
}