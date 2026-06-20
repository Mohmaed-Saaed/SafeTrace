using SafeTrace.Application.DTOs.Auth;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<RegisterDto, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false));
        }
    }
}