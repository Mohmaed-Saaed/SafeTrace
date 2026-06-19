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
                 .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FName))
                 .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LName))
                 .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                 .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.IsVerified))
                 .ForMember(dest => dest.HomeLocationLatitude, opt => opt.MapFrom(src => src.HomeLocationLatitude))
                 .ForMember(dest => dest.HomeLocationLongitude, opt => opt.MapFrom(src => src.HomeLocationLongitude))
                 .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(src => src.ProfileImage)).ReverseMap();

            CreateMap<ApplicationUser, GetAllDTO>()
                 .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                 .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FName))
                 .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LName))
                 .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                 .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.IsVerified))
                 .ForMember(dest => dest.HomeLocationLatitude, opt => opt.MapFrom(src => src.HomeLocationLatitude))
                 .ForMember(dest => dest.HomeLocationLongitude, opt => opt.MapFrom(src => src.HomeLocationLongitude))
                 .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(src => src.ProfileImage)).ReverseMap();

            CreateMap<UpdateProfileInfoDTO, ApplicationUser>()
                .ForMember(dest => dest.FName, opt => opt.MapFrom(src => src.FName))
                .ForMember(dest => dest.LName, opt => opt.MapFrom(src => src.LName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.IsVerified))
                .ForMember(dest => dest.HomeLocationLatitude, opt => opt.MapFrom(src => src.HomeLocationLatitude))
                .ForMember(dest => dest.HomeLocationLongitude, opt => opt.MapFrom(src => src.HomeLocationLongitude))
                .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(src => src.ProfileImage)).ReverseMap();

            CreateMap<SendNotificationDTO, Notification>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type)).ReverseMap();

            CreateMap<GetUserNotificationsDTO, Notification>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type)).ReverseMap();


        }
    }
}