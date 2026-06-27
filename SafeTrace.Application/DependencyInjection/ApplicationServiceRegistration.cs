using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Services;

namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<INotificationServices, NotificationService>();
            services.AddAutoMapper(cfg => { cfg.AddProfile<MappingProfile>(); });

            services.AddAutoMapper(cfg => { }, typeof(ApplicationServiceRegistration).Assembly);

            services.AddScoped<IAIMatchingService, AIMatchingService>();

            return services;
        }
    }
}