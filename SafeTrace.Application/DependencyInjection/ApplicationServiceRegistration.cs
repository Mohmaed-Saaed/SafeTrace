using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Interfaces.IServices.INotificationSewrvice;
using SafeTrace.Application.Interfaces.IServices.IUserProfile;
using SafeTrace.Application.Services;
using SafeTrace.Application.Services.Cases;
using SafeTrace.Application.Services.NotificationServices;
using SafeTrace.Application.Services.UserProfileServices;
using SafeTrace.Infrastructure.Service.Founded;

namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ICaseHelperService, CaseHelperService>();
            services.AddScoped<ICasesService, CasesService>();
            services.AddScoped<ILongTermCaseService, LongTermCaseService>();  
            services.AddScoped<IUnknownCaseService,UnKnownCaseService>();
            services.AddScoped<IUrgentCaseService, UrgentCaseService>();

            services.AddScoped<IFoundedService, FoundedService>();

            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<INotificationServices, NotificationService>();

            services.AddScoped<IChatService,ChatService>();
            services.AddScoped<IMessageService, MessageService>();

            services.AddScoped<IAIMatchingService, AIMatchingService>();

            services.AddAutoMapper(cfg => { }, typeof(ApplicationServiceRegistration).Assembly);
            
            return services;
        }
    }
}