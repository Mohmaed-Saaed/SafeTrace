using SafeTrace.Application.DTOs.UnKnownCase.Request;

namespace SafeTrace.Application.Mapping
{
    public class UnKnownProfile : Profile
    {
        public UnKnownProfile()
        {
            CreateMap<CreateUnknownDto, UnknownCase>()
               .ForMember(dest => dest.Photos, opt => opt.Ignore());

            CreateMap<UpdateUnknownCaseDto, UnknownCase>()
               .ForMember(dest => dest.Photos, opt => opt.Ignore())
               .ForAllMembers(opt =>
               opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
