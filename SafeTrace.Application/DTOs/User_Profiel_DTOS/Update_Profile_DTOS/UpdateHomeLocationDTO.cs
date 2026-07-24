using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateHomeLocationDTO
    {
        [Required(ErrorMessage = "خط العرض للمنزل مطلوب.")]
        [Range(-90, 90, ErrorMessage = "خط العرض يجب أن يكون بين -90 و 90.")]
        public double? HomeLatitude { get; set; }

        [Required(ErrorMessage = "خط الطول للمنزل مطلوب.")]
        [Range(-180, 180, ErrorMessage = "خط الطول يجب أن يكون بين -180 و 180.")]
        public double? HomeLongitude { get; set; }
    }
}