using SafeTrace.Application.DTOs.FacebookPages.Response;

namespace SafeTrace.Application.Mapping
{
    public class FacebookPageMappingProfile : Profile
    {
        public FacebookPageMappingProfile()
        {
            CreateMap<FacebookPage, FacebookPageResponseDto>()
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email));
        }
    }
}