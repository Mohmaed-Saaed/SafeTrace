using SafeTrace.Application.Interfaces;
using SafeTrace.Infrastructure.Service.Founded;

namespace SafeTrace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            //builder.Services.AddScoped<IDBInitializer, DBInitializer>();
            //builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IFoundedService, FoundedService>();

            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<FoundedProfile>();
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500",
                                    "http://localhost:5501", "http://127.0.0.1:5501", "https://localhost:7204", "https://localhost:5173", "https://localhost:7126",
                                    "http://localhost:3000", "http://localhost:8080") // Add common dev ports
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed((host) => true); // More permissive for development
                });
            });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            //builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("CorsPolicy");


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseAuthorization();

            //using (var scope = app.Services.CreateScope())
            //{
            //    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDBInitializer>();
            //    dbInitializer.Initialize();
            //}
            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "api/{controller}/{action=Index}/{id?}");
            app.Run();
        }
    }
}
