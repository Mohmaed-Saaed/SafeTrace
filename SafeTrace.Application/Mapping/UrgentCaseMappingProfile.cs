using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;
using SafeTrace.Application.DTOs.UrgentMissingCase.Response;

namespace SafeTrace.Application.Mapping
{
    public class UrgentCaseMappingProfile : Profile
    {
        public UrgentCaseMappingProfile()
        {
            CreateMap<CasePhoto, CasePhotoDto>();
            CreateMap<FoundPersonInfo, FoundPersonInfoDto>();
            CreateMap<AgeCategory, AgeCategoryDto>();
            CreateMap<ApplicationUser, UserDto>();

            CreateMap<UrgentCase, UrgentCaseListItemDto>()
                .ForMember(dest => dest.MainPhoto, opt => opt.MapFrom(src => src.Photos.Where(p => p.IsPrimary).Select(p => p.ImagePath).FirstOrDefault()));

            CreateMap<UrgentCase, UrgentCaseDetailDto>()
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location != null ? src.Location.Y : (double?)null))
                .ForMember(dest => dest.Longitude,opt => opt.MapFrom(src => src.Location != null ? src.Location.X : (double?)null))
                .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos));

            CreateMap<UrgentCaseCreateDto, UrgentCase>()
                .ForMember(dest => dest.Photos, opt => opt.Ignore())
                .ForMember(dest => dest.CaseType, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LimitReachDate, opt => opt.Ignore())
                .ForMember(dest => dest.CaseCode, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Chats, opt => opt.Ignore())
                .ForMember(dest => dest.FoundPersonInfo, opt => opt.Ignore())
                .ForMember(dest => dest.AgeCategory, opt => opt.Ignore());

            CreateMap<UrgentCaseUpdateDto, UrgentCase>()
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Latitude.HasValue && src.Longitude.HasValue ? new Point(src.Longitude.Value, src.Latitude.Value){ SRID = 4326 }: null))
                .ForMember(dest => dest.Photos, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Chats, opt => opt.Ignore())
                .ForMember(dest => dest.FoundPersonInfo, opt => opt.Ignore())
                .ForMember(dest => dest.AgeCategory, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UrgentCase, UrgentCaseAdminDto>()
                .ForMember(dest => dest.FullName,opt => opt.MapFrom(src => $"{src.FName} {src.SName} {src.TName} {src.LName}"))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location != null ? src.Location.Y : (double?)null))
                .ForMember(dest => dest.Longitude,opt => opt.MapFrom(src => src.Location != null ? src.Location.X : (double?)null))
                .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos))
                .ForMember(dest => dest.FoundPersonInfo, opt => opt.MapFrom(src => src.FoundPersonInfo));

        }
    }
}