using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SafeTrace.Application.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SafeTrace.Infrastructure.DependencyInjection
{
    public static class JwtServiceRegistration
    {
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<JwtOptions>(configuration.GetSection("JWT"));

            var jwtOptions = configuration.GetSection("JWT").Get<JwtOptions>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = jwtOptions!.Audience,
                    ValidIssuer = jwtOptions.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

                        var userId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                    ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                        var jwtId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(jwtId))
                        {
                            context.Fail("Invalid token claims.");
                            return;
                        }

                        var cacheKey = $"session:{userId}:{jwtId}";
                        var cachedSessionJson = await cache.GetStringAsync(cacheKey);

                        if (string.IsNullOrWhiteSpace(cachedSessionJson))
                        {
                            context.Fail("Session not found or expired from cache.");
                            return;
                        }

                        var session = JsonSerializer.Deserialize<UserSession>(cachedSessionJson);

                        if (session is null || session.IsRevoked)
                        {
                            context.Fail("Session has been revoked.");
                            return;
                        }
                    }
                };
            });

            return services;
        }
    }
}