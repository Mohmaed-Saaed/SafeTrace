using SafeTrace.Application.DTOs.Founded.Response;
using SafeTrace.Application.Helpers;
using SafeTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Mapping
{
    public class FoundedProfile : Profile
    {
        public FoundedProfile()
        {
            CreateMap<FoundPersonInfo, FoundPersonListItemDto>()
.ForMember(
dest => dest.Name,
opt => opt.MapFrom(src =>
NameHelper.CombineNames(
src.Case.FName,
src.Case.SName
)))
.ForMember(
dest => dest.Image, opt => opt.MapFrom(src => src.Case.Photos.Where(p => p.IsPrimary).Select(p => p.ImagePath).FirstOrDefault()))
.ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Case.AgeCategory.Name));
        }
    }
}
