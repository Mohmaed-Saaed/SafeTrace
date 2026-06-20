using Microsoft.Extensions.DependencyInjection;
namespace SafeTrace.Application.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { }, typeof(ApplicationServiceRegistration).Assembly);
            services.AddScoped<IChatService,ChatService>();
            services.AddScoped<IMessageService, MessageService>();

            return services;
        }
    }
}