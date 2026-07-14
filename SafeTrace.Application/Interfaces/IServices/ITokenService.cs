using SafeTrace.Domain.Entities;
using System.Security.Claims;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, string role);
        RefreshToken GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}