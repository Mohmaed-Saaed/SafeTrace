using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;

namespace SafeTrace.Application.Mapping
{
    public class FacebookPageMappingProfile : Profile
    {
        public FacebookPageMappingProfile()
        {
            CreateMap<CreateFacebookPageDto, FacebookPage>()
                .ForMember(dest => dest.FacebookPageId, opt => opt.MapFrom(src => src.FacebookPageId.Trim()))
                .ForMember(dest => dest.PageName, opt => opt.MapFrom(src => src.PageName.Trim()))
                .ForMember(dest => dest.PageUrl, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.PageUrl) ? null : src.PageUrl.Trim()))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.PageAccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.TokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.IntegrationStatus, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.LastSyncedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ImportedPosts, opt => opt.Ignore());

            CreateMap<UpdateFacebookPageDto, FacebookPage>()
                .ForMember(dest => dest.PageName, opt => opt.MapFrom(src => src.PageName.Trim()))
                .ForMember(dest => dest.PageUrl, opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.PageUrl) ? null : src.PageUrl.Trim()))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FacebookPageId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.PageAccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.TokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.IntegrationStatus, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.LastSyncedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ImportedPosts, opt => opt.Ignore());

            CreateMap<FacebookPage, FacebookPageResponseDto>()
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email));
        }
    }
}