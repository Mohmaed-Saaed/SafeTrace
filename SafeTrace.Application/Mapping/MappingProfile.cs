using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.User.Response;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS.Update_Profile_DTOS;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Mapping
{
    public partial class MappingProfile : Profile
    {
        public MappingProfile()
        {




            CreateMap<Case, MyCaseListItemDto>()
    .ForMember(d => d.FullName,
        o => o.MapFrom(s =>
            $"{s.FName} {s.SName} {s.TName} {s.LName}".Trim()));

            CreateMap<ApplicationUser, GetUserInfoDTO>()
    .ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => $"{src.FName} {src.LName}"))
    .ForMember(dest => dest.HomeLatitude,
        opt => opt.MapFrom(src => src.HomeLocationLatitude))
    .ForMember(dest => dest.HomeLongitude,
        opt => opt.MapFrom(src => src.HomeLocationLongitude))
    .ForMember(dest => dest.Cases,
        opt => opt.MapFrom(src => src.Cases));

            CreateMap<ApplicationUser, VisitUserDTO>()
    .ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => $"{src.FName} {src.LName}"));


            #region test method dto
            CreateMap<ApplicationUser, GetAllDTO>()
      .ForMember(dest => dest.FullName,
          opt => opt.MapFrom(src => $"{src.FName} {src.LName}"))
      .ForMember(dest => dest.HomeLocation,
          opt => opt.MapFrom(src =>
              $"{src.HomeLocationLatitude},{src.HomeLocationLongitude}"))
      .ReverseMap();

            #endregion

            CreateMap<UpdateNameDTO, ApplicationUser>()
                 .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FirstName))
                 .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LastName))
                 .ReverseMap();

            CreateMap<UpdateHomeLocationDTO, ApplicationUser>()
                .ForMember(dest => dest.HomeLocationLatitude, opt => opt.MapFrom(src => src.HomeLatitude))
                .ForMember(dest => dest.HomeLocationLongitude, opt => opt.MapFrom(src => src.HomeLongitude))
                .ReverseMap();

            CreateMap<UpdateProfileImageDTO, ApplicationUser>();
            CreateMap<AddIdImageDTO, ApplicationUser>();
            CreateMap<ChangePhoneNumberDTO, ApplicationUser>();



            CreateMap<UpdateProfileInfoDTO, ApplicationUser>()
                .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.HomeLocationLatitude,
        opt => opt.MapFrom(src => src.HomeLatitude))
    .ForMember(dest => dest.HomeLocationLongitude,
        opt => opt.MapFrom(src => src.HomeLongitude))
                .ReverseMap();



            CreateMap<SendNotificationDTO, Notification>().ReverseMap();

            CreateMap<Notification, GetUserNotificationsDTO>().ReverseMap();

            CreateMap<SendToAllNotificationDTO, Notification>().ReverseMap();



        }
    }
}