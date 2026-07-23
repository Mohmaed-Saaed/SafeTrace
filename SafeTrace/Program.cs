using ElmahCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using SafeTrace.API.ExceptionHandlers;
using SafeTrace.API.ExtensionMethods;
using Audit.Core;
using Audit.EntityFramework;
using System.Security.Claims;
using SafeTrace.API.Hubs;
using SafeTrace.Application.DependencyInjection;
using SafeTrace.Application.Hubs;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Interfaces.IServices.ICases;
using SafeTrace.Application.Services.Cases;
using SafeTrace.Infrastructure.DependencyInjection;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using NetTopologySuite.Geometries;
using System.Text.Json;

namespace SafeTrace.API
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
            builder.Services.AddSignalR()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter());
                }); builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .WithOrigins("https://localhost:4200", "http://localhost:5500", "http://127.0.0.1:5500",
                                    "http://localhost:5501", "http://127.0.0.1:5501", "https://localhost:7204", "https://localhost:5173", "https://localhost:7126",
                                    "http://localhost:3000", "http://localhost:8080",
                                    "https://leqaaweb.runasp.net"
                                    , "https://rearview-manual-coke.ngrok-free.dev") // Add common dev ports
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

            // Background Services
            builder.Services.AddScoped<ICaseCleanupService, CaseCleanupService>();

            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHangfireServer();


            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .AuditTypeMapper(t => typeof(AuditLog))
                    .AuditEntityAction<AuditLog>((ev, entry, entity) =>
                    {
                        var realChanges = entry.Changes?.Where(c => !Equals(c.OriginalValue, c.NewValue)).ToList();

                        entity.TableName = entry.Table;
                        entity.Type = entry.Action;
                        var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                        entity.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
                        entity.UserId = ev.CustomFields.ContainsKey("UserId") ? ev.CustomFields["UserId"]?.ToString() : null;
                        entity.PrimaryKey = string.Join(",", entry.PrimaryKey.Values);

                        if (entry.Action == "Insert")
                        {
                            // entity.NewValues = entry.ColumnValues != null ? System.Text.Json.JsonSerializer.Serialize(entry.ColumnValues) : null;
                            if (entry.ColumnValues != null)
                            {
                                var values = entry.ColumnValues.ToDictionary(
                                    x => x.Key,
                                    x => NormalizeAuditValue(x.Value));

                                entity.NewValues = JsonSerializer.Serialize(values);
                            }
                            else
                            {
                                entity.NewValues = null;
                            }
                            entity.OldValues = null;
                            entity.AffectedColumns = null;
                        }
                        else if (entry.Action == "Delete")
                        {
                            entity.NewValues = null;
                            if (entry.ColumnValues != null)
                            {
                                var values = entry.ColumnValues.ToDictionary(
                                    x => x.Key,
                                    x => NormalizeAuditValue(x.Value));

                                entity.OldValues = JsonSerializer.Serialize(values);
                            }
                            else
                            {
                                entity.OldValues = null;
                            }
                            // entity.OldValues = entry.ColumnValues != null ? System.Text.Json.JsonSerializer.Serialize(entry.ColumnValues) : null;
                            entity.AffectedColumns = null;
                        }
                        else
                        {
                            if (realChanges?.Count > 0)
                            {
                                var oldValues = realChanges.ToDictionary(
                                    c => c.ColumnName,
                                    c => NormalizeAuditValue(c.OriginalValue));

                                var newValues = realChanges.ToDictionary(
                                    c => c.ColumnName,
                                    c => NormalizeAuditValue(c.NewValue));

                                entity.OldValues = JsonSerializer.Serialize(oldValues);
                                entity.NewValues = JsonSerializer.Serialize(newValues);

                                entity.AffectedColumns = string.Join(", ", realChanges.Select(c => c.ColumnName));
                            }
                            else
                            {
                                entity.OldValues = null;
                                entity.NewValues = null;
                                entity.AffectedColumns = null;
                            }
                            // entity.OldValues = realChanges?.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(realChanges.ToDictionary(c => c.ColumnName, c => c.OriginalValue)) : null;
                            // entity.NewValues = realChanges?.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(realChanges.ToDictionary(c => c.ColumnName, c => c.NewValue)) : null;
                            // entity.AffectedColumns = realChanges?.Count > 0 ? string.Join(", ", realChanges.Select(c => c.ColumnName)) : null;
                        }
                    })
                    .IgnoreMatchedProperties(true));

            Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
            {
                var httpContext = new HttpContextAccessor().HttpContext;
                scope.Event.CustomFields["UserId"] = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            });

            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
            {
                var efEvent = scope.Event.GetEntityFrameworkEvent();
                if (efEvent != null)
                {
                    var httpContext = new HttpContextAccessor().HttpContext;
                    bool shouldLog = false;

                    if (httpContext != null)
                    {
                        var endpoint = httpContext.GetEndpoint();
                        if (endpoint != null)
                        {
                            var targetPermissions = new[]
                            {
                                "Users.Reject", "Users.Approve", "Users.ToggleBlock", "Users.ChangeRole", "Users.RegisterByAdmin", "Users.AssignPermissions",
                                "Roles.Create", "Roles.Delete", "Roles.UpdateRolePermissions",
                                "Complaints.MarkAsSolved", "Complaints.HardDelete",
                                "UnknownCases.Approve", "UnknownCases.Reject", "UnknownCases.HardDelete",
                                "LongTermCases.Approve", "LongTermCases.Reject", "LongTermCases.HardDelete",
                                "UrgentCases.Approve", "UrgentCases.Reject", "UrgentCases.HardDelete"
                            };

                            var authorizeAttributes = endpoint.Metadata.OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
                            
                            foreach (var attr in authorizeAttributes)
                            {
                                if (!string.IsNullOrEmpty(attr.Policy) && targetPermissions.Contains(attr.Policy))
                                {
                                    shouldLog = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!shouldLog)
                    {
                        scope.Discard();
                        return;
                    }

                    var ignoredTables = new[]
                    {
                        "RefreshTokens", "UserOtps", "Notifications",
                        "Messages", "Chats", "AiSearchUsages",
                        "AspNetUserTokens", "AspNetUserLogins",
                        "DuplicateGroups", "DuplicateGroupCases",
                        "FoundPersonInfos", "CaseFiles",
                        "AgeCategories", "Donations"
                    };

                    efEvent.Entries.RemoveAll(e => ignoredTables.Contains(e.Table));

                    if (efEvent.Entries.Count == 0)
                    {
                        scope.Discard();
                    }
                }
            });

            var app = builder.Build();

            // Custom Basic Auth Middleware for Elmah
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/elmah"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    var expectedUser = builder.Configuration["Elmah:Username"];
                    var expectedPass = builder.Configuration["Elmah:Password"];
                    var expectedAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{expectedUser}:{expectedPass}"));

                    if (authHeader != $"Basic {expectedAuth}")
                    {
                        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Elmah Secure Area\"";
                        context.Response.StatusCode = 401;
                        return;
                    }
                }
                await next();
            });

            app.UseElmah();
            app.UseExceptionHandler();
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

            var hangfireUsername = builder.Configuration["Hangfire:Username"];
            var hangfirePassword = builder.Configuration["Hangfire:Password"];

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = false,
                    SslRedirect = false,
                    LoginCaseSensitive = true,
                    Users = new []
                    {
                        new BasicAuthAuthorizationUser
                        {
                            Login = hangfireUsername,
                            PasswordClear = hangfirePassword
                        }
                    }
                })}
            });

            RecurringJob.AddOrUpdate<IAuthCleanupService>("CleanupExpiredOtps", service => service.CleanupExpiredOtpsAsync(), Cron.Daily);
            RecurringJob.AddOrUpdate<IAuthCleanupService>("CleanupOldRefreshTokens", service => service.CleanupOldRefreshTokensAsync(), Cron.Daily);
            RecurringJob.AddOrUpdate<ICaseCleanupService>("CleanupExpiredUrgentCases", service => service.CleanupExpiredUrgentCasesAsync(), Cron.Hourly);


            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("CorsPolicy");

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationsHub>("/SafeTrace.Application/Hubs/notifications");
            app.MapHub<ChatHub>("/chatHub");


            app.Run();

            static object? NormalizeAuditValue(object? value)
            {
                return value switch
                {
                    Point p => new
                    {
                        Latitude = p.Y,
                        Longitude = p.X
                    },

                    double d when double.IsNaN(d) || double.IsInfinity(d) => null,

                    float f when float.IsNaN(f) || float.IsInfinity(f) => null,

                    _ => value
                };
            }
        }
    }
}