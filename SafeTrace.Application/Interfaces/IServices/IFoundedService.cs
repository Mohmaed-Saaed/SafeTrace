using SafeTrace.Application.DTOs.Founded.Request;
using SafeTrace.Application.DTOs.Founded.Response;
using SafeTrace.Application.DTOs.FoundedDTO.Response;

namespace SafeTrace.Application.Interfaces
{
    public interface IFoundedService
    {
        public Task<PaginationResponseDto<FoundPersonListItemDto>> GetAllAsync(FoundedHeaderQueryDTO query);
        public Task<ApiResponse<PostDetailsResponseDTO>> GetDetailsAsync(long id);
    }
}