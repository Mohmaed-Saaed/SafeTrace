using SafeTrace.API.ExceptionHandlers;
using SafeTrace.Application.DependencyInjection;
using SafeTrace.Infrastructure.DependencyInjection;
using Serilog;
using System.Text.Json.Serialization;

namespace SafeTrace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(builder.Configuration)
                        .CreateLogger();

            builder.Host.UseSerilog();

            // Add services to the container.

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddControllers()
                            .AddJsonOptions(options =>
                            {
                                options.JsonSerializerOptions.Converters.Add(
                                    new JsonStringEnumConverter());
                            });

            builder.Services.AddSwaggerGen();

            //builder.Services.AddScoped<IDBInitializer, DBInitializer>();
            //builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddApplication();

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

            app.UseExceptionHandler();

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