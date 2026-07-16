using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.Helpers;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Mapping
{
    public class AiMatchingProfile : Profile
    {
        public AiMatchingProfile()
        {
            CreateMap<Case, MatchedCaseDto>()
                .IncludeBase<Case, CaseListItemBaseDto>()
                .ForMember(dest => dest.Similarity, opt => opt.Ignore());
        }
    }
}