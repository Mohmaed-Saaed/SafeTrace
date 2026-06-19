using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces;
using SafeTrace.Infrastructure.Service.Founded;
namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { }, typeof(ApplicationServiceRegistration).Assembly);

            services.AddScoped<IFoundedService, FoundedService>();

            return services;
        }
    }
}