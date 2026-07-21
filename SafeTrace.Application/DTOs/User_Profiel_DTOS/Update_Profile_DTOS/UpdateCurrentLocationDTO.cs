using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class UpdateCurrentLocationDTO
    {
        public double? CurrentLocationLatitude { get; set; }

        public double? CurrentLocationLongitude { get; set; }
    }
}
