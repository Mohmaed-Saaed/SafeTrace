using SafeTrace.Application.DTOs.CasesMissing.Response;

namespace SafeTrace.Application.Mapping
{

    public class CaseMappingProfile : Profile
    {
        public CaseMappingProfile()
        {
            CreateMap<Case, CasesDto>();
        }
    }

}
