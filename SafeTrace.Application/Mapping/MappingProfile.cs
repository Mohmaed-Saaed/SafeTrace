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
                 .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FName} {src.LName}"))
                 .ForMember(x => x.Email, opt => opt.MapFrom(src => src.Email))
                 .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.EmailConfirmed))
                 .ForMember(dest => dest.IdentificationImage, opt => opt.MapFrom(src => src.IdentificationImage))
                 .ForMember(dest => dest.VerificationStatus, opt => opt.MapFrom(src => src.VerificationStatus))
                .ForMember(dest => dest.HomeLocation,
                     opt => opt.MapFrom(src =>
                      $"{src.HomeLocationLatitude},{src.HomeLocationLongitude}"))
                 .ReverseMap();

            #region test method dto
            CreateMap<ApplicationUser, GetAllDTO>()
                 .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                 .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FName},{src.LName}"))
                 .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                 .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.EmailConfirmed))
                 .ForMember(dest => dest.IdentificationImage, opt => opt.MapFrom(src => src.IdentificationImage))
                 .ForMember(dest => dest.VerificationStatus, opt => opt.MapFrom(src => src.VerificationStatus))
                 .ForMember(dest => dest.HomeLocation,
                     opt => opt.MapFrom(src =>
                      $"{src.HomeLocationLatitude},{src.HomeLocationLongitude}"))
                 .ReverseMap();
            #endregion

            CreateMap<UpdateProfileInfoDTO, ApplicationUser>()
                .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.IdentificationImage, opt => opt.MapFrom(src => src.IdentificationImage))
                .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(src => src.ProfileImage)).ReverseMap();

            CreateMap<SendNotificationDTO, Notification>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content));
            //.ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type)).ReverseMap();

            CreateMap<GetUserNotificationsDTO, Notification>()
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type)).ReverseMap();

            CreateMap<SendToAllNotificationDTO, Notification>()
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type)).ReverseMap();


        }
    }
}