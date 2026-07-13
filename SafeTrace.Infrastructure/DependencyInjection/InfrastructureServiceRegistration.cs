using ElmahCore.Mvc;
using ElmahCore.Sql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SafeTrace.Application.Interfaces;
using SafeTrace.Application.Services;
using SafeTrace.Infrastructure.Authorization;
using SafeTrace.Infrastructure.Filters;
using SafeTrace.Infrastructure.Options;
using SafeTrace.Infrastructure.Persistence;
using System.Text;

namespace SafeTrace.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), x => x.UseNetTopologySuite()));

            services.AddScoped<IDBInitializer, DBInitializer>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IRolePermissionService, RolePermissionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFaceRecognitionService, FaceRecognitionService>();
            services.AddHttpClient<IPaymentService, PaymentService>();

            var awsOptions = configuration.GetAWSOptions("AWS");
            var accessKey = configuration["AWS:AccessKey"];
            var secretKey = configuration["AWS:SecretKey"];

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            }

            services.AddDefaultAWSOptions(awsOptions);
            services.AddAWSService<Amazon.Rekognition.IAmazonRekognition>();

            services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
            services.Configure<MailSettingsOptions>(configuration.GetSection("MailSettings"));

            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();

            
            services.AddScoped<IComplaintService, ComplaintService>();

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

            services.AddAuthorization();

            services.AddElmah<SqlErrorLog>(options =>
            {
                options.Path = "/elmah";

                options.ConnectionString = configuration.GetConnectionString("DefaultConnection");

                options.Filters.Add(new BusinessExceptionFilter());

                //options.OnPermissionCheck = context =>
                //    context.User.Identity != null &&
                //    context.User.Identity.IsAuthenticated &&
                //    context.User.IsInRole("Admin");
            });

            return services;
        }
    }
}