using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.Helpers;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Mapping
{
    public class AiMatchingProfile : Profile
    {
        public AiMatchingProfile()
        {
            CreateMap<Case, MatchedCaseDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    NameHelper.CombineNames(src.FName, src.SName, src.TName, src.LName, "غير معروف")))
                
                .ForMember(dest => dest.MainPhotoPath, opt => opt.MapFrom(src =>
                    src.Photos.FirstOrDefault(p => p.IsPrimary) != null
                        ? src.Photos.FirstOrDefault(p => p.IsPrimary)!.ImagePath
                        : src.Photos.FirstOrDefault()!.ImagePath))
                
                .ForMember(dest => dest.Similarity, opt => opt.Ignore());
        }
    }
}