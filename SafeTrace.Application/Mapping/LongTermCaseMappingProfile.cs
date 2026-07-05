using SafeTrace.Application.DTOs.LongTermCases.Request;
using SafeTrace.Application.DTOs.Cases.Response;
using SafeTrace.Application.DTOs.LongTermCases.Response;

namespace SafeTrace.Application.Mapping
{
    public class LongTermCaseMappingProfile : Profile
    {
        public LongTermCaseMappingProfile()
        {
            CreateMap<LongTermMissingCase, LongTermCaseListDto>()
                .IncludeBase<Case, CaseListItemBaseDto>();

            CreateMap<LongTermMissingCase, LongTermCaseDetailDto>()
                .IncludeBase<Case, CaseDetailBaseDto>();

            CreateMap<CreateLongTermCaseDto, LongTermMissingCase>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Status, opt => opt.Ignore())
                .ForMember(d => d.CaseCode, opt => opt.Ignore())
                .ForMember(d => d.CreatedAt, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.CaseType, opt => opt.Ignore())
                .ForMember(d => d.FoundPersonInfo, opt => opt.Ignore())
                .ForMember(d => d.Chats, opt => opt.Ignore())
                .ForMember(d => d.CaseFiles, opt => opt.Ignore())
                .ForMember(d => d.PoliceReportImage, opt => opt.Ignore());
        }
    }
}
