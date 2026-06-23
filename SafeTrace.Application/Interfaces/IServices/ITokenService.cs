using SafeTrace.Domain.Entities;
using System.Security.Claims;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface ITokenService
    {
        string GenerateAccessToken(ApplicationUser user, IList<string> roles);
        RefreshToken GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}