using SafeTrace.Application.DTOs.Founded.Response;
using SafeTrace.Application.DTOs.FoundedDTO.Response;
using SafeTrace.Application.Helpers;
using SafeTrace.Domain.Entities;

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
                            src.Case.SName)))
                .ForMember(
                    dest => dest.Image,
                    opt => opt.MapFrom(src =>
                        src.Case.Photos
                            .Where(p => p.IsPrimary)
                            .Select(p => p.ImagePath)
                            .FirstOrDefault()))
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.Id))
                .ForMember(
                    dest => dest.Age,
                    opt => opt.MapFrom(src => src.Case.AgeCategory.Name))
                .ForMember(
                    dest => dest.CaseId,
                    opt => opt.MapFrom(src => src.Case.Id))
                .ForMember(
                    dest => dest.FoundedAt,
                    opt => opt.MapFrom(src => src.FoundedAt));


            CreateMap<FoundPersonInfo, PostDetailsResponseDTO>()
    .ForMember(
        d => d.FullName,
        opt => opt.MapFrom(s => $"{s.Case.FName} {s.Case.SName}"))
    .ForMember(
        d => d.MainImage,
        opt => opt.MapFrom(s =>
            s.Case.Photos.Select(p => p.ImagePath).FirstOrDefault()))
    .ForMember(
        d => d.Age,
        opt => opt.MapFrom(s => s.Case.Age))
    .ForMember(
        d => d.Gender,
        opt => opt.MapFrom(s => s.Case.Gender.ToString()))
    .ForMember(
        d => d.FoundDescription,
        opt => opt.MapFrom(s => s.Description))
    .ForMember(
        d => d.MissingDescription,
        opt => opt.MapFrom(s => s.Case.Description))
    .ForMember(
        d => d.FoundLocation,
        opt => opt.MapFrom(s => $"{s.Government} - {s.City} - {s.Street}"))
    .ForMember(
        d => d.MissingLocation,
        opt => opt.MapFrom(s =>
            $"{s.Case.Government} - {s.Case.City} - {s.Case.Street}"))
    .ForMember(d => d.FounedDate,
        opt => opt.MapFrom(s => s.FoundedAt));
        }
        
    }
}