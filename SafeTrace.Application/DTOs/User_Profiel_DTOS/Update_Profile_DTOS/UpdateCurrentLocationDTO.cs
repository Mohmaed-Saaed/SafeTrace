using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateCurrentLocationDTO
    {
        [Required(ErrorMessage = "خط العرض الحالي مطلوب.")]
        [Range(-90, 90, ErrorMessage = "خط العرض يجب أن يكون بين -90 و 90.")]
        public double? CurrentLocationLatitude { get; set; }

        [Required(ErrorMessage = "خط الطول الحالي مطلوب.")]
        [Range(-180, 180, ErrorMessage = "خط الطول يجب أن يكون بين -180 و 180.")]
        public double? CurrentLocationLongitude { get; set; }
    }
}