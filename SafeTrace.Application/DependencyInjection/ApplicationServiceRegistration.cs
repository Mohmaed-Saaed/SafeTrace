using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Mapping;
using SafeTrace.Application.Services;

namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { cfg.AddProfile<MappingProfile>(); });
            services.AddScoped<IUnknownCaseService, UnKnownCaseService>();
            return services;
        }
    }
}