using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeTrace.Application.Interfaces;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Interfaces.IRepositories;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using SafeTrace.Infrastructure.DataAccess;
using SafeTrace.Infrastructure.Persistence;
using SafeTrace.Infrastructure.Repositories.UnitOfWork;
using SafeTrace.Infrastructure.Services;

namespace SafeTrace.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),x => x.UseNetTopologySuite())  );
            
            services.AddScoped<IDBInitializer, DBInitializer>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }
    }
}