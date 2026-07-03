using NetTopologySuite.Geometries;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.UrgentCase.Request;
using SafeTrace.Application.DTOs.UrgentCase.Response;


namespace SafeTrace.Application.Mapping
{
    public class UrgentCaseMappingProfile : Profile
    {
        public UrgentCaseMappingProfile()
        {
            CreateMap<UrgentCase, UrgentCaseListDto>()
                .IncludeBase<Case, CaseListItemBaseDto>();

            CreateMap<UrgentCase, UrgentCaseDetailDto>()
                .IncludeBase<Case, CaseDetailBaseDto>()
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location == null ? (double?)null : src.Location.Y))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Location == null ? (double?)null : src.Location.X));

            CreateMap<UrgentCase, UrgentCaseCreateResponse>();
            CreateMap<UrgentCase, UrgentCaseUpdateResponse>();

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
        }
    }
}
