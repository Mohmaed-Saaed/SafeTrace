using System.Text;
using ElmahCore.Mvc;
using ElmahCore.Sql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SafeTrace.Application.Interfaces;
using SafeTrace.Application.Interfaces.IServices.common;
using SafeTrace.Application.Constants;
using SafeTrace.Application.Services;
using SafeTrace.Infrastructure.Authorization;
using SafeTrace.Infrastructure.Options;
using SafeTrace.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace SafeTrace.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            
            services.AddAppDbContext(configuration);
            services.AddAppDataProtection(configuration);
            services.AddAppServices();
            services.AddAppAws(configuration);
            
            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.Configure<MailSettingsOptions>(configuration.GetSection("MailSettings"));
            services.Configure<FacebookGraphOptions>(configuration.GetSection(FacebookGraphOptions.SectionName));
            services.Configure<GeocodingOptions>(configuration.GetSection(GeocodingOptions.SectionName));
            services.Configure<BedrockOptions>(configuration.GetSection(BedrockOptions.SectionName));

            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            services.AddAppIdentity();
            services.AddAppAuthentication(configuration);
            services.AddAuthorization();

            services.AddElmah<SqlErrorLog>(options =>
            {
                options.Path = "/elmah";
                options.ConnectionString = configuration.GetConnectionString("DefaultConnection");
                options.OnPermissionCheck = context => true;
            });

            services.AddAppRateLimiting();

            return services;
        }

        private static IServiceCollection AddAppDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), x => x.UseNetTopologySuite()));
            
            return services;
        }

        private static IServiceCollection AddAppDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            var dataProtectionBuilder = services.AddDataProtection()
                .SetApplicationName("SafeTrace");

            var keysPath = configuration["DataProtection:KeysPath"];
            if (string.IsNullOrWhiteSpace(keysPath))
            {
                keysPath = Path.Combine(AppContext.BaseDirectory, "DataProtection-Keys");
            }

            Directory.CreateDirectory(keysPath);
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

            return services;
        }

        private static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<IDBInitializer, DBInitializer>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddHttpContextAccessor();
            services.AddScoped<IImageUrlService, ImageUrlService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IRolePermissionService, RolePermissionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthCleanupService, AuthCleanupService>();
            services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();
            services.AddHttpClient<IPaymentService, PaymentService>();
            services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
            services.AddScoped<IExcelGeneratorService, ExcelGeneratorService>();
            services.AddScoped<IComplaintService, ComplaintService>();
            services.AddScoped<IAiCaseAnalyzerService, AiCaseAnalyzerService>();
            services.AddHttpClient<IFacebookGraphService, FacebookGraphService>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<FacebookGraphOptions>>().Value;
                client.BaseAddress = CreateAbsoluteBaseUri(options.BaseUrl, FacebookGraphOptions.SectionName);
            }).RemoveAllLoggers();

            services.AddHttpClient<IGeocodingService, GoogleGeocodingService>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<GeocodingOptions>>().Value;
                client.BaseAddress = CreateAbsoluteBaseUri(options.BaseUrl, GeocodingOptions.SectionName);
            }).RemoveAllLoggers();

            services.AddHttpClient<IExternalImageDownloadService, ExternalImageDownloadService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            })
            .RemoveAllLoggers();
            
            return services;
        }

        private static Uri CreateAbsoluteBaseUri(string baseUrl, string sectionName)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
                !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The {sectionName} base URL must be a valid HTTPS URL.");
            }

            return new Uri(baseUri.AbsoluteUri.TrimEnd('/') + '/');
        }

        private static IServiceCollection AddAppAws(this IServiceCollection services, IConfiguration configuration)
        {
            var awsOptions = configuration.GetAWSOptions("AWS");
            var accessKey = configuration["AWS:AccessKey"];
            var secretKey = configuration["AWS:SecretKey"];

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            }

            services.AddDefaultAWSOptions(awsOptions);
            services.AddAWSService<Amazon.Rekognition.IAmazonRekognition>();
            services.AddAWSService<Amazon.S3.IAmazonS3>();

            var bedrockRegion = configuration["AWS:Bedrock:Region"];
            if (!string.IsNullOrEmpty(bedrockRegion))
            {
                var bedrockOptions = configuration.GetAWSOptions("AWS");
                bedrockOptions.Region = Amazon.RegionEndpoint.GetBySystemName(bedrockRegion);
                services.AddAWSService<Amazon.BedrockRuntime.IAmazonBedrockRuntime>(bedrockOptions);
            }
            else
            {
                services.AddAWSService<Amazon.BedrockRuntime.IAmazonBedrockRuntime>();
            }

            return services;
        }

        private static IServiceCollection AddAppIdentity(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }

        private static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.SaveToken = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions?.Issuer,
                    ValidAudience = jwtOptions?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions?.SecretKey!)),
                    ClockSkew = TimeSpan.Zero
                };

                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                             (path.StartsWithSegments("/chatHub") ||
                              path.StartsWithSegments("/SafeTrace.Application/Hubs/notifications")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Unauthorized",
                            Detail = "أنت غير مسجل الدخول، أو أن الجلسة غير صالحة أو منتهية.",
                            Instance = context.Request.Path
                        };

                        await context.Response.WriteAsJsonAsync(problemDetails);
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = "Forbidden",
                            Detail = "ليس لديك الصلاحيات الكافية لتنفيذ هذا الإجراء.",
                            Instance = context.Request.Path
                        };

                        await context.Response.WriteAsJsonAsync(problemDetails);
                    }
                };
            });

            return services;
        }

        private static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = 429,
                        Title = "Too Many Requests",
                        Detail = "لقد تجاوزت الحد المسموح به. يرجى المحاولة لاحقاً.",
                        Instance = context.HttpContext.Request.Path
                    });
                };

                options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy(RateLimitPolicies.AuthLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(15)
                        }));

                options.AddPolicy(RateLimitPolicies.AiLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 30,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy(RateLimitPolicies.OtpRequestLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(15)
                        }));

                options.AddPolicy(RateLimitPolicies.OtpSubmitLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(15)
                        }));

                options.AddPolicy(RateLimitPolicies.ComplaintsLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 3,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(60)
                        }));

                options.AddPolicy(RateLimitPolicies.ProfileUpdateLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 15,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(15)
                        }));

                options.AddPolicy(RateLimitPolicies.ChatLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 60,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                options.AddPolicy(RateLimitPolicies.PaymentLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(15)
                        }));

                options.AddPolicy(RateLimitPolicies.CreateCaseLimit, httpContext =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5,
                            QueueLimit = 0,
                            // Window = TimeSpan.FromHours(1) // return to this when testing is done
                            Window = TimeSpan.FromMinutes(1) // in testing
                        }));
            });

            return services;
        }
    }
}
