using SafeTrace.Application.DTOs.UnKnownDtos;
using SafeTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Mapping
{
    public class UnKnownProfile : Profile
    {
        public UnKnownProfile()
        {
            CreateMap<CreateUnknownDto, UnknownCase>()
               .ForMember(dest => dest.Photos, opt => opt.Ignore());

            CreateMap<UnknownCase, GetUnknownDto>()
               .ForMember(dest => dest.FullName,
               opt => opt.MapFrom(src =>
                string.Join(" ",
                    new[]
                    {
                    src.FName,
                    src.SName,
                    src.TName,
                    src.LName
            }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                )))
              .ForMember(dest => dest.Photos,
              opt => opt.MapFrom(src =>
                src.Photos.Select(p => p.ImagePath).ToList()));
        }
    }
}
