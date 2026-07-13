using ElmahCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.API.ExceptionHandlers;
using SafeTrace.API.ExtensionMethods;
using SafeTrace.API.Hubs;
using SafeTrace.Application.DependencyInjection;
using SafeTrace.Application.Hubs;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Infrastructure.DependencyInjection;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;

namespace SafeTrace
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(builder.Configuration)
                        .CreateLogger();

            builder.Host.UseSerilog();

            // Add services to the container.

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddControllers()
                            .AddJsonOptions(options =>
                            {
                                options.JsonSerializerOptions.Converters.Add(
                                    new JsonStringEnumConverter());
                            });

            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                options.IncludeXmlComments(xmlPath);
            });

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplication();
            builder.Services.AddSignalR();
            builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .WithOrigins("https://localhost:4200", "http://localhost:5500", "http://127.0.0.1:5500",
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

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            builder.Services.AddSignalR();

            var app = builder.Build();

            app.UseExceptionHandler();
            app.UseElmah();
            app.UseStatusCodePages(async context =>
            {
                var response = context.HttpContext.Response;

                if (response.StatusCode == 404)
                {
                    await response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = 404,
                        Title = "Not Found",
                        Detail = "لم يتم العثور على المسار المطلوب.",
                        Instance = context.HttpContext.Request.Path
                    });
                }
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }



            await app.SeedDataAsync();
            await app.ApplyPendingMigrationsAsync();
            await app.SetupAwsResourcesAsync();


            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("CorsPolicy");

            app.UseRateLimiter(); // Apply Rate Limiting before Auth
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationsHub>("/SafeTrace.Application/Hubs/notifications");
            app.MapHub<ChatHub>("/chatHub");


            app.Run();
        }
    }
}