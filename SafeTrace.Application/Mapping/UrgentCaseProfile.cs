
using SafeTrace.Application.DTOs.UrgentMissingCase;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Mapping{
    public class UrgentCaseProfile : Profile
    {
        public UrgentCaseProfile()
        {
            CreateMap<UrgentCase, UrgentCaseListItemDto>()
                .ForMember(dest => dest.FullName, 
                        opt => opt.MapFrom(src => string.Join(" ", new[] { src.FName, src.SName, src.TName, src.LName }
                            .Where(x => !string.IsNullOrWhiteSpace(x)))))
                .ForMember(dest => dest.MainPhotoUrl,
                        opt => opt.MapFrom(src => src.Photos
                            .Where(p => p.IsPrimary)
                            .Select(p => p.ImagePath)
                            .FirstOrDefault()));


            CreateMap<UrgentCase, UrgentCaseDetailDto>()
                .ForMember(dest => dest.AgeCategory, 
                    obt => obt.MapFrom(src => src.AgeCategory.Name))
                .ForMember(dest => dest.Photos, 
                    obt => obt.MapFrom(src => src.Photos.Select(photo => photo.ImagePath).ToList()));
        }
    }
}
