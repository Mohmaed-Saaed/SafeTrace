using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Mapping;

namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { 
                cfg.AddProfile<MappingProfile>();
                cfg.AddProfile<FoundedProfile>();
            });
            return services;
        }
    }
}