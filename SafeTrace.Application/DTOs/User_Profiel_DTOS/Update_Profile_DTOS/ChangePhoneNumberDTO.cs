using System;
using System.Collections.Generic;
using System.Text;

using SafeTrace.Application.Common.Validators.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS
{
    public class ChangePhoneNumberDTO
    {
        [Required(ErrorMessage = "رقم الهاتف مطلوب.")]
        [EgyptianPhone(ErrorMessage = "يرجى إدخال رقم هاتف مصري صحيح.")]
        public string PhoneNumber { get; set; } = string.Empty;

    }
}
