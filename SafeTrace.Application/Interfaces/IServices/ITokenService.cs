using SafeTrace.Domain.Entities;
using SafeTrace.Application.DTOs.Auth;
using System.Security.Claims;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface ITokenService
    {
        Task<JwtTokenResult> CreateJwtTokenAsync(ApplicationUser user);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}