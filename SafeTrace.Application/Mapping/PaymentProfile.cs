using SafeTrace.Application.DTOs.Founded.Response;
using SafeTrace.Application.DTOs.Payment.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Mapping
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {

            CreateMap<Donation, DonationAdminListDto>()
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : "Guest"))
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.PaidAt, opt => opt.MapFrom(src => src.PaidAt ?? DateTime.MinValue));

        }
    }
}
