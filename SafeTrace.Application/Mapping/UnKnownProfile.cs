using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.UnKnownCase.Request;
using SafeTrace.Application.DTOs.UnKnownCase.Response;

namespace SafeTrace.Application.Mapping
{
    public class UnKnownProfile : Profile
    {
        public UnKnownProfile()
        {
            CreateMap<UnknownCase, UnknownCaseListDto>()
                .IncludeBase<Case, CaseListItemBaseDto>();

            CreateMap<UnknownCase, UnknownCaseDetailDto>()
                .IncludeBase<Case, CaseDetailBaseDto>();

            CreateMap<CreateUnknownDto, UnknownCase>()
               .ForMember(dest => dest.Photos, opt => opt.Ignore());

            CreateMap<UpdateUnknownCaseDto, UnknownCase>()
               .ForMember(dest => dest.Photos, opt => opt.Ignore())
               .ForAllMembers(opt =>
               opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
