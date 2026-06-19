using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Application.Mapping;
using SafeTrace.Application.Services.NotificationServices;
using SafeTrace.Application.Services.UserProfileServices;
using SafeTrace.Domain.Interfaces.IRepositories;

namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<INotificationServices, NotificationService>();
            services.AddAutoMapper(cfg => { cfg.AddProfile<MappingProfile>(); });

            return services;
        }
    }
}