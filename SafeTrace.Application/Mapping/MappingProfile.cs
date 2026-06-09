using SafeTrace.Application.DTOs.Founded;
using SafeTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeTrace.Application.Mapping
{
    public partial class MappingProfile : Profile
    {
        public MappingProfile()
        {
            FoundedProfile();
        }
        public void FoundedProfile()
        {
            CreateMap<FoundPersonInfo, DTOFoundedIndexResponse>()
                .ForMember(
                    dest => dest.Name,
                    opt => opt.MapFrom(src =>
                        $"{src.Case.FName} {src.Case.SName}")
                )
                .ForMember(
                    dest => dest.Image,
                    opt => opt.MapFrom(src => src.Case.Photos)
                );
        }
    }
}
