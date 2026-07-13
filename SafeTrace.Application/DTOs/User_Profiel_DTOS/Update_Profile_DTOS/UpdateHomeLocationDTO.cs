using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateHomeLocationDTO
    {

        [Required(ErrorMessage = "HomeLocation is required.")]
        public double? HomeLatitude { get; set; }

        [Required(ErrorMessage = "HomeLocation is required.")]
        public double? HomeLongitude { get; set; }


    }
}
