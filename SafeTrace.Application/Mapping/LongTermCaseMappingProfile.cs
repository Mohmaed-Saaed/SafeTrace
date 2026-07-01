using SafeTrace.Application.DTOs.LongTermCases.Request;

namespace SafeTrace.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for the Long-Term Missing Cases module.
    /// Kept in its own file/class (separate from the existing MappingProfile) to avoid merge conflicts.
    /// NOTE: this profile must be registered once in Program.cs:
    ///   cfg.AddProfile&lt;LongTermCaseMappingProfile&gt;();
    /// </summary>
    public class LongTermCaseMappingProfile : Profile
    {
        public LongTermCaseMappingProfile()
        {
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
                .ForMember(d => d.Photos, opt => opt.Ignore())
                .ForMember(d => d.PoliceReportImage, opt => opt.Ignore());
        }
    }
}
