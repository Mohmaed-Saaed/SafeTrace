using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.SharedCases;

namespace SafeTrace.Application.Mapping
{

    public class CaseMappingProfile : Profile
    {
        public CaseMappingProfile()
        {
            CreateMap<Case, CasesFilterDto>();
            CreateMap<CasePhoto, CasePhotoDto>();
            CreateMap<FoundPersonInfo, FoundPersonInfoDto>();
            CreateMap<AgeCategory, AgeCategoryDto>();
            CreateMap<ApplicationUser, UserDto>();

            CreateMap<Case, CaseListItemDto>()
                .ForMember(dest => dest.MainPhoto, opt => opt.MapFrom(src => src.Photos.Where(p => p.IsPrimary).Select(p => p.ImagePath).FirstOrDefault()));

            CreateMap<Case, CaseDetailDto>()
                .ForMember(d => d.Photos, o => o.MapFrom(s => s.Photos))
                .ForMember(d => d.FoundPersonInfo, o => o.MapFrom(s => s.FoundPersonInfo))
                .ForMember(d => d.AgeCategory, o => o.MapFrom(s => s.AgeCategory))
                .ForMember(d => d.User, o => o.MapFrom(s => s.User));

            CreateMap<UrgentCase, CaseDetailDto>()
                .IncludeBase<Case, CaseDetailDto>()
                .ForMember(d => d.Latitude, o => o.MapFrom(s => (double?)s.Location.Y))
                .ForMember(d => d.Longitude, o => o.MapFrom(s => (double?)s.Location.X));
        }
    }

}
