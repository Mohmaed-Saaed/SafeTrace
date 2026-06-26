using SafeTrace.Application.DTOs.UnKnownDtos;
using SafeTrace.Application.Helpers;
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
                src.Photos.Select(p => p.ImagePath).ToList())).ForMember(
        dest => dest.AgeCategory,
        opt => opt.MapFrom(src =>
            AgeCategoryHelper.GetCategory(src.Age))); 

            CreateMap<CasePhoto, UnknownPhotoDto>()
           .ForMember(d => d.PhotoId, o => o.MapFrom(s => s.Id))
           .ForMember(d => d.ImagePath, o => o.MapFrom(s => s.ImagePath))
          .ForMember(d => d.IsPrimary, o => o.MapFrom(s => s.IsPrimary));

            CreateMap<UnknownCase, GetUnknownDto>();

            CreateMap<UpdateUnkownCaseDto, UnknownCase>()
               .ForMember(dest => dest.Photos, opt => opt.Ignore())
               .ForAllMembers(opt =>
               opt.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UnknownCase, UnKnownCaseFilterDto>().ForMember(dest => dest.FullName,
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
                ))).ForMember(dest => dest.Photos,
              opt => opt.MapFrom(src =>
                src.Photos.Select(p => p.ImagePath).ToList()));


            CreateMap<UnknownCase, GetMyUnknownnCasesDto>()
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
