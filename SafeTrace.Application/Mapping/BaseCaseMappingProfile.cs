using SafeTrace.Application.DTOs.Cases.Request;
using SafeTrace.Application.DTOs.Cases.Response;

namespace SafeTrace.Application.Mapping
{
    public class BaseCaseMappingProfile : Profile
    {
        public BaseCaseMappingProfile()
        {
            CreateMap<Case, CasesFilterBaseDto>();
            CreateMap<CaseFile, CasePhotoDto>();
            CreateMap<FoundPersonInfo, FoundPersonInfoDto>();
            CreateMap<AgeCategory, AgeCategoryDto>();
            CreateMap<ApplicationUser, UserDto>();

            CreateMap<Case, CaseListItemBaseDto>()
                .ForMember(dest => dest.MainPhoto, opt => opt.MapFrom(src =>
                    src.CaseFiles
                        .OrderByDescending(p => p.IsPrimary)
                        .Select(p => p.ImagePath)
                        .FirstOrDefault()));

            CreateMap<Case, CaseDetailBaseDto>()
                .ForMember(d => d.Photos, o => o.MapFrom(s => s.CaseFiles))
                .ForMember(d => d.FoundPersonInfo, o => o.MapFrom(s => s.FoundPersonInfo))
                .ForMember(d => d.AgeCategory, o => o.MapFrom(s => s.AgeCategory))
                .ForMember(d => d.User, o => o.MapFrom(s => s.User));

            CreateMap<FoundPersonInfoRequestDto, FoundPersonInfo>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CaseId, opt => opt.Ignore())
                .ForMember(dest => dest.FoundedUserId, opt => opt.Ignore())
                .ForMember(dest => dest.FoundedUser, opt => opt.Ignore())
                .ForMember(dest => dest.Case, opt => opt.Ignore());
        }
    }

}
