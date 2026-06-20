using SafeTrace.Application.DTOs.Auth.Request;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;

namespace SafeTrace.Application.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<RegisterDto, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.VerificationStatus, opt => opt.MapFrom(src => VerificationStatus.Unverified));

            CreateMap<ApplicationUser, GetUserDto>();
        }
    }
}