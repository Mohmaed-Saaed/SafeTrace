using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;

namespace SafeTrace.Application.Interfaces.IServices
{
    public interface IFacebookPageService
    {
        Task<FacebookPageResponseDto> CreateAsync(CreateFacebookPageDto dto);
        Task<FacebookPageResponseDto> UpdateAsync(long id, UpdateFacebookPageDto dto);
        Task<List<FacebookPageResponseDto>> GetAllAsync();
        Task<FacebookPageResponseDto> GetByIdAsync(long id);
        Task<FacebookPageResponseDto> ConnectAsync(long id);
        Task<FacebookPageResponseDto> ReconnectAsync(long id);
        Task<FacebookPageResponseDto> DisconnectAsync(long id);
    }
}
