using SafeTrace.Application.DTOs.NotificationDTOS;
using SafeTrace.Application.DTOs.User_Profiel_DTOS;
using SafeTrace.Domain.Entities;

namespace SafeTrace.Application.Mapping
{
    public partial class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, GetUserInfoDTO>()
        .ForMember(dest => dest.FullName,
            opt => opt.MapFrom(src => $"{src.FName} {src.LName}"))
        .ForMember(dest => dest.HomeLatitude,
        opt => opt.MapFrom(src => src.HomeLocationLatitude))
    .ForMember(dest => dest.HomeLongitude,
        opt => opt.MapFrom(src => src.HomeLocationLongitude));


            #region test method dto
            CreateMap<ApplicationUser, GetAllDTO>()
      .ForMember(dest => dest.FullName,
          opt => opt.MapFrom(src => $"{src.FName} {src.LName}"))
      .ForMember(dest => dest.HomeLocation,
          opt => opt.MapFrom(src =>
              $"{src.HomeLocationLatitude},{src.HomeLocationLongitude}"))
      .ReverseMap();

            #endregion

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