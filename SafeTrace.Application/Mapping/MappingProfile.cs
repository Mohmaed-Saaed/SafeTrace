using SafeTrace.Application.DTOs;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Mapping
{
    public partial class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateUnknownDto, UnknownCase>()
           .ForMember(dest => dest.Photos, opt => opt.Ignore());
        
        }
    }
}