using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Models.Facebook;

namespace SafeTrace.Application.Services
{
    public class FacebookPageService : IFacebookPageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFacebookGraphService _facebookGraphService;

        public FacebookPageService(
            IUnitOfWork unitOfWork,
            IFacebookGraphService facebookGraphService)
        {
            _unitOfWork = unitOfWork;
            _facebookGraphService = facebookGraphService;
        }

        public async Task<FacebookPageResponseDto> CreateAsync(CreateFacebookPageDto dto)
        {
            var facebookPageId = dto.FacebookPageId.Trim();
            var userId = dto.UserId.Trim();

            if (await _unitOfWork.Repository<FacebookPage>()
                .AnyAsync(page => page.FacebookPageId == facebookPageId))
            {
                throw new ConflictException("A Facebook page with this FacebookPageId already exists.");
            }

            await EnsureUserExistsAsync(userId);

            var page = new FacebookPage
            {
                FacebookPageId = facebookPageId,
                PageName = dto.PageName.Trim(),
                PageUrl = NormalizeOptional(dto.PageUrl),
                UserId = userId,
                PageAccessToken = null,
                TokenExpiresAt = null,
                IntegrationStatus = FacebookIntegrationStatus.Disconnected,
                IsActive = false,
                LastSyncedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<FacebookPage>().CreateAsync(page);
            await _unitOfWork.SaveAsync();

            return MapToResponse(page);
        }

        public async Task<FacebookPageResponseDto> UpdateAsync(long id, UpdateFacebookPageDto dto)
        {
            var page = await GetEntityAsync(id);
            var userId = dto.UserId.Trim();

            if (!string.Equals(page.UserId, userId, StringComparison.Ordinal))
                await EnsureUserExistsAsync(userId);

            page.PageName = dto.PageName.Trim();
            page.PageUrl = NormalizeOptional(dto.PageUrl);
            page.UserId = userId;
            page.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveAsync();

            return MapToResponse(page);
        }

        public async Task<List<FacebookPageResponseDto>> GetAllAsync()
        {
            return await _unitOfWork.Repository<FacebookPage>()
                .Query(tracked: false)
                .OrderByDescending(page => page.CreatedAt)
                .Select(page => new FacebookPageResponseDto
                {
                    Id = page.Id,
                    FacebookPageId = page.FacebookPageId,
                    PageName = page.PageName,
                    PageUrl = page.PageUrl,
                    UserId = page.UserId,
                    IntegrationStatus = page.IntegrationStatus,
                    IsActive = page.IsActive,
                    TokenExpiresAt = page.TokenExpiresAt,
                    LastSyncedAt = page.LastSyncedAt,
                    CreatedAt = page.CreatedAt,
                    UpdatedAt = page.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<FacebookPageResponseDto> GetByIdAsync(long id)
        {
            var page = await _unitOfWork.Repository<FacebookPage>()
                .GetOneAsync(item => item.Id == id, tracked: false);

            if (page is null)
                throw new NotFoundException($"Facebook page with id {id} was not found.");

            return MapToResponse(page);
        }

        public async Task<FacebookPageResponseDto> ConnectAsync(long id)
        {
            var page = await GetEntityAsync(id);

            if (page.IntegrationStatus == FacebookIntegrationStatus.Connected)
                throw new BadRequestException("This Facebook page is already connected.");

            var connection = await _facebookGraphService.ConnectPageAsync(page.FacebookPageId);
            ApplySuccessfulConnection(page, connection);

            await _unitOfWork.SaveAsync();

            return MapToResponse(page);
        }

        public async Task<FacebookPageResponseDto> ReconnectAsync(long id)
        {
            var page = await GetEntityAsync(id);

            if (page.IntegrationStatus != FacebookIntegrationStatus.NeedsReconnect &&
                page.IntegrationStatus != FacebookIntegrationStatus.Disconnected)
            {
                throw new BadRequestException(
                    "Only disconnected Facebook pages or pages that need reconnection can be reconnected.");
            }

            var connection = await _facebookGraphService.ReconnectPageAsync(page.FacebookPageId);
            ApplySuccessfulConnection(page, connection);

            await _unitOfWork.SaveAsync();

            return MapToResponse(page);
        }

        public async Task<FacebookPageResponseDto> DisconnectAsync(long id)
        {
            var page = await GetEntityAsync(id);

            page.IntegrationStatus = FacebookIntegrationStatus.Disconnected;
            page.IsActive = false;
            page.PageAccessToken = null;
            page.TokenExpiresAt = null;
            page.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveAsync();

            return MapToResponse(page);
        }

        private async Task<FacebookPage> GetEntityAsync(long id)
        {
            var page = await _unitOfWork.Repository<FacebookPage>().GetByIdAsync(id);

            return page
                ?? throw new NotFoundException($"Facebook page with id {id} was not found.");
        }

        private async Task EnsureUserExistsAsync(string userId)
        {
            var userExists = await _unitOfWork.Repository<ApplicationUser>()
                .AnyAsync(user => user.Id == userId);

            if (!userExists)
                throw new NotFoundException($"User with id {userId} was not found.");
        }

        private static void ApplySuccessfulConnection(
            FacebookPage page,
            FacebookPageConnectionResult connection)
        {
            if (!string.Equals(
                    page.FacebookPageId,
                    connection.FacebookPageId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(connection.PageAccessToken))
            {
                throw new BadRequestException(
                    "Facebook Graph API did not validate access to the configured Facebook page.");
            }

            page.PageAccessToken = connection.PageAccessToken;
            page.TokenExpiresAt = connection.TokenExpiresAt;
            page.IntegrationStatus = FacebookIntegrationStatus.Connected;
            page.IsActive = true;
            page.UpdatedAt = DateTime.UtcNow;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static FacebookPageResponseDto MapToResponse(FacebookPage page)
        {
            return new FacebookPageResponseDto
            {
                Id = page.Id,
                FacebookPageId = page.FacebookPageId,
                PageName = page.PageName,
                PageUrl = page.PageUrl,
                UserId = page.UserId,
                IntegrationStatus = page.IntegrationStatus,
                IsActive = page.IsActive,
                TokenExpiresAt = page.TokenExpiresAt,
                LastSyncedAt = page.LastSyncedAt,
                CreatedAt = page.CreatedAt,
                UpdatedAt = page.UpdatedAt
            };
        }
    }
}
