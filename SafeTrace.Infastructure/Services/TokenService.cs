using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SafeTrace.Application.DTOs.Auth;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Application.Options;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SafeTrace.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDistributedCache _cache;

        public TokenService(
            IOptions<JwtOptions> jwtOptions,
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IDistributedCache cache)
        {
            _jwtOptions = jwtOptions.Value;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<JwtTokenResult> CreateJwtTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var jwtId = Guid.NewGuid().ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, jwtId),
                new Claim("IsVerified", user.IsVerified.ToString().ToLower())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.DurationInMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                SigningCredentials = creds,
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(securityToken);

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var session = new UserSession
            {
                UserId = user.Id,
                JwtId = jwtId,
                IsRevoked = false,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };

            await _unitOfWork.UserSessionRepository.CreateAsync(session);
            await _unitOfWork.SaveAsync();

            var cacheKey = $"session:{user.Id}:{jwtId}";
            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpiration = session.ExpiresAtUtc };
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(session), cacheOptions);

            return new JwtTokenResult
            {
                Token = jwtToken,
                RefreshToken = refreshToken,
                JwtId = jwtId,
                ExpiresAtUtc = expiresAt
            };
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}