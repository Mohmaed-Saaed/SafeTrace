using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Cases.Response;

namespace SafeTrace.Application.Mapping
{

    public class BaseCaseMappingProfile : Profile
    {
        public BaseCaseMappingProfile()
        {
            CreateMap<Case, CasesFilterBaseDto>();
            CreateMap<CasePhoto, CasePhotoDto>();
            CreateMap<FoundPersonInfo, FoundPersonInfoDto>();
            CreateMap<AgeCategory, AgeCategoryDto>();
            CreateMap<ApplicationUser, UserDto>();

            CreateMap<Case, CaseListItemBaseDto>()
                .ForMember(dest => dest.MainPhoto, opt => opt.MapFrom(src =>
                    src.Photos
                        .OrderByDescending(p => p.IsPrimary)
                        .Select(p => p.ImagePath)
                        .FirstOrDefault()));

            CreateMap<Case, CaseDetailBaseDto>()
                .ForMember(d => d.Photos, o => o.MapFrom(s => s.Photos))
                .ForMember(d => d.FoundPersonInfo, o => o.MapFrom(s => s.FoundPersonInfo))
                .ForMember(d => d.AgeCategory, o => o.MapFrom(s => s.AgeCategory))
                .ForMember(d => d.User, o => o.MapFrom(s => s.User));
        }
    }

}
