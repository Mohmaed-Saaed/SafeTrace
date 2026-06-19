using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Mapping;
using SafeTrace.Application.Services;

namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { }, typeof(ApplicationServiceRegistration).Assembly);

            services.AddScoped<IUrgentCaseService, UrgentCaseService>();
            return services;
        }
    }
}