using AutoMapper;
using SafeTrace.Application.Helpers;
using SafeTrace.Application.DTOs.LongTermCases;
using SafeTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            CreateMap<CasePhoto, CasePhotoDto>();
            CreateMap<FoundPersonInfo, FoundPersonInfoDto>();

            CreateMap<LongTermMissingCase, LongTermCaseCardDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => BuildFullName(s)))
                .ForMember(d => d.AgeCategory, opt => opt.MapFrom(s => AgeCategoryHelper.GetCategory(s.Age)))
                .ForMember(d => d.MissingDate, opt => opt.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.MainPhoto, opt => opt.MapFrom(s => s.Photos.Select(p => p.ImagePath).FirstOrDefault()));

            CreateMap<LongTermMissingCase, LongTermCaseDetailsDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => BuildFullName(s)))
                .ForMember(d => d.AgeCategory, opt => opt.MapFrom(s => AgeCategoryHelper.GetCategory(s.Age)))
                .ForMember(d => d.ReporterId, opt => opt.MapFrom(s => s.UserId))
                .ForMember(d => d.ReporterUserName, opt => opt.MapFrom(s => s.User != null ? s.User.UserName : null));

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

        private static string? BuildFullName(Case c)
        {
            var parts = new[] { c.FName, c.SName, c.TName, c.LName }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var full = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(full) ? null : full;
        }
    }
}
