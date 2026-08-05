using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;

namespace SafeTrace.Application.Mapping
{
    public partial class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Case, MyCaseListItemDto>()
                .ForMember(
                    dest => dest.FullName,
                    opt => opt.MapFrom(s =>
                        (s.FName ?? "") +
                        (string.IsNullOrEmpty(s.SName) ? "" : " " + s.SName) +
                        (string.IsNullOrEmpty(s.TName) ? "" : " " + s.TName) +
                        (string.IsNullOrEmpty(s.LName) ? "" : " " + s.LName)))
                .ForMember(dest => dest.AgeCategory, 
                    opt => opt.MapFrom(src => src.AgeCategory))
                .ForMember(dest => dest.MainImageUrl, 
                    opt => opt.MapFrom(src => src.CaseFiles.FirstOrDefault(f => f.IsPrimary)!.ImagePath))
                .ForMember(dest => dest.FoundPersonInfoId, 
                    opt => opt.MapFrom(src => src.FoundPersonInfo != null ? src.FoundPersonInfo.Id : (long?)null));

            CreateMap<ApplicationUser, GetUserInfoDTO>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FName} {src.LName}"))
                .ForMember(dest => dest.HomeLatitude,
                    opt => opt.MapFrom(src => src.HomeLocation == null ? (double?)null : src.HomeLocation.Y))
                .ForMember(dest => dest.HomeLongitude,
                    opt => opt.MapFrom(src => src.HomeLocation == null ? (double?)null : src.HomeLocation.X))
                .ForMember(dest => dest.Cases,
                    opt => opt.MapFrom(src => src.Cases));


            CreateMap<ApplicationUser, VisitUserDTO>()
                .ForMember(d => d.Email,
                    o => o.MapFrom(s => s.Email))
                .ForMember(d => d.PhoneNumber,
                    o => o.MapFrom(s => s.PhoneNumber))
                .ForMember(d => d.HomeLatitude,
                    o => o.MapFrom(s => s.HomeLocation == null ? (double?)null : s.HomeLocation.Y))
                .ForMember(d => d.HomeLongitude,
                    o => o.MapFrom(s => s.HomeLocation == null ? (double?)null : s.HomeLocation.X))
                .ForMember(d => d.FullName,
                    o => o.MapFrom(s => $"{s.FName} {s.LName}"));



            #region test method dto
            CreateMap<ApplicationUser, GetAllDTO>()
                .ForMember(dest => dest.FullName,
                    opt => opt.MapFrom(src => $"{src.FName} {src.LName}"))
                .ForMember(dest => dest.HomeLocation,
                    opt => opt.MapFrom(src => src.HomeLocation == null ? null : $"{src.HomeLocation.Y},{src.HomeLocation.X}"))
                .ReverseMap();

            #endregion

            CreateMap<UpdateNameDTO, ApplicationUser>()
                 .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FirstName))
                 .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LastName))
                 .ReverseMap();

            CreateMap<UpdateProfileImageDTO, ApplicationUser>();
            CreateMap<AddIdImageDTO, ApplicationUser>();
            CreateMap<ChangePhoneNumberDTO, ApplicationUser>();



            CreateMap<UpdateProfileInfoDTO, ApplicationUser>()
                .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.HomeLocation,
                    opt => opt.MapFrom(src =>
                        src.HomeLatitude.HasValue && src.HomeLongitude.HasValue
                            ? new NetTopologySuite.Geometries.Point(src.HomeLongitude.Value, src.HomeLatitude.Value) { SRID = 4326 }
                            : null))
                .ReverseMap();



            CreateMap<SendNotificationDTO, Notification>().ReverseMap();

            CreateMap<Notification, GetUserNotificationsDTO>().ReverseMap();

            CreateMap<SendToAllNotificationDTO, Notification>().ReverseMap();



        }
    }
}
