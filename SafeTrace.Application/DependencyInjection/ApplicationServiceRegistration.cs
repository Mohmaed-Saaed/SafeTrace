using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces;
using SafeTrace.Infrastructure.Service.Founded;
using SafeTrace.Application.Mapping;
using SafeTrace.Application.Services;

namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { }, typeof(ApplicationServiceRegistration).Assembly);

            services.AddScoped<IFoundedService, FoundedService>();


            services.AddScoped<IUrgentCaseService, UrgentCaseService>();
            return services;
        }
    }
}