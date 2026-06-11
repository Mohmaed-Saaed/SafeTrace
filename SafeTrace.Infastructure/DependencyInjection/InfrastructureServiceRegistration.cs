using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Repositories.UnitOfWork;

namespace SafeTrace.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            return services;
        }
    }
}