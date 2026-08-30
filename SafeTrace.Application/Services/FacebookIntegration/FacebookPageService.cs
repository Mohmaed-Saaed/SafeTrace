using Microsoft.AspNetCore.DataProtection;
using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Exceptions;

namespace SafeTrace.Application.Services.FacebookIntegration
{
    public class FacebookPageService : IFacebookPageService
    {
        private const string FacebookTokenPurpose = "SafeTrace.FacebookPageAccessToken";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFacebookGraphService _facebookGraphService;
        private readonly IDataProtector _dataProtector;
        private readonly IMapper _mapper;

        public FacebookPageService(
            IUnitOfWork unitOfWork,
            IFacebookGraphService facebookGraphService,
            IDataProtectionProvider dataProtectionProvider,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _facebookGraphService = facebookGraphService;
            _dataProtector = dataProtectionProvider.CreateProtector(FacebookTokenPurpose);
            _mapper = mapper;
        }

        public async Task<ApiResponse<FacebookPageResponseDto>> IntegrateAsync(IntegrateFacebookPageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AccessToken))
            {
                throw new BadRequestException("رمز وصول صفحة Facebook مطلوب.");
            }

            var user = await GetUserByEmailAsync(dto.UserEmail);

            var pageProfile = await _facebookGraphService.GetPageProfileAsync(dto.AccessToken);

            var pageExists = await _unitOfWork.Repository<FacebookPage>()
                .AnyAsync(page => page.FacebookPageId == pageProfile.PageId);

            if (pageExists)
            {
                throw new ConflictException("صفحة Facebook بهذا المعرّف مسجلة بالفعل.");
            }

            var tokenExpiresAt = await _facebookGraphService.DebugTokenAsync(dto.AccessToken);

            var encryptedToken = _dataProtector.Protect(dto.AccessToken.Trim());

            var page = new FacebookPage
            {
                FacebookPageId = pageProfile.PageId,
                PageName = pageProfile.PageName,
                PageUrl = pageProfile.PageUrl,
                PageAccessToken = encryptedToken,
                TokenExpiresAt = tokenExpiresAt,
                UserId = user.Id,
                IntegrationStatus = FacebookIntegrationStatus.Connected,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<FacebookPage>().CreateAsync(page);
            await _unitOfWork.SaveAsync();

            var result = _mapper.Map<FacebookPageResponseDto>(page);
            return ApiResponse<FacebookPageResponseDto>.Ok(result, "تم ربط صفحة Facebook بنجاح.");
        }

        public async Task<ApiResponse<List<FacebookPageResponseDto>>> GetAllAsync(FacebookPageFilterDto? filter = null)
        {
            var query = _unitOfWork
                .Repository<FacebookPage>()
                .Query(tracked: false)
                .Include(page => page.User)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    var searchTerm = filter.Search.Trim();
                    query = query.Where(page =>
                        page.PageName.Contains(searchTerm) ||
                        (page.User != null && page.User.Email != null && page.User.Email.Contains(searchTerm)));
                }

                if (filter.IntegrationStatus.HasValue)
                {
                    query = query.Where(page => page.IntegrationStatus == filter.IntegrationStatus.Value);
                }
            }

            var pages = await query
                .OrderByDescending(page => page.CreatedAt)
                .ToListAsync();

            var result = _mapper.Map<List<FacebookPageResponseDto>>(pages);
            return ApiResponse<List<FacebookPageResponseDto>>.Ok(result, "تم جلب صفحات Facebook بنجاح.");
        }

        public async Task<ApiResponse<string>> DisconnectAsync(long id)
        {
            var page = await GetEntityWithUserAsync(id);

            if (page.IntegrationStatus == FacebookIntegrationStatus.Disconnected)
            {
                throw new BadRequestException("صفحة Facebook غير متصلة بالفعل.");
            }

            page.IntegrationStatus = FacebookIntegrationStatus.Disconnected;
            page.IsActive = false;
            page.PageAccessToken = null;
            page.TokenExpiresAt = null;
            page.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(message: "تم إلغاء ربط صفحة Facebook بنجاح.");
        }

        public async Task<ApiResponse<FacebookPageResponseDto>> ReconnectAsync(long id, ReconnectFacebookPageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AccessToken))
            {
                throw new BadRequestException("رمز الوصول إلى Facebook مطلوب.");
            }

            var page = await GetEntityWithUserAsync(id);

            var pageProfile = await _facebookGraphService.GetPageProfileAsync(dto.AccessToken);

            if (!string.Equals(page.FacebookPageId, pageProfile.PageId, StringComparison.Ordinal))
            {
                throw new BadRequestException("رمز الوصول المقدم لا ينتمي إلى صفحة Facebook المحددة.");
            }

            var tokenExpiresAt = await _facebookGraphService.DebugTokenAsync(dto.AccessToken);

            page.PageName = pageProfile.PageName;
            if (!string.IsNullOrWhiteSpace(pageProfile.PageUrl))
            {
                page.PageUrl = pageProfile.PageUrl;
            }

            page.PageAccessToken = _dataProtector.Protect(dto.AccessToken.Trim());
            page.TokenExpiresAt = tokenExpiresAt;
            page.IntegrationStatus = FacebookIntegrationStatus.Connected;
            page.IsActive = true;
            page.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveAsync();

            var result = _mapper.Map<FacebookPageResponseDto>(page);
            return ApiResponse<FacebookPageResponseDto>.Ok(result, "تمت إعادة ربط صفحة Facebook بنجاح.");
        }

        public async Task<ApiResponse<string>> DeleteAsync(long id)
        {
            var page = await _unitOfWork.Repository<FacebookPage>().GetByIdAsync(id);

            if (page is null)
            {
                throw new NotFoundException($"لم يتم العثور على صفحة Facebook بالمعرّف {id}.");
            }

            var hasImportedPosts = await _unitOfWork.Repository<FacebookImportedPost>()
                .AnyAsync(post => post.FacebookPageId == id);

            if (hasImportedPosts)
            {
                throw new ConflictException("لا يمكن حذف صفحة Facebook لوجود منشورات مستوردة مرتبطة بها.");
            }

            _unitOfWork.Repository<FacebookPage>().Remove(page);
            await _unitOfWork.SaveAsync();

            return ApiResponse<string>.Ok(null, "تم حذف صفحة Facebook بنجاح.");
        }

        private async Task<FacebookPage> GetEntityWithUserAsync(long id)
        {
            var page = await _unitOfWork
                .Repository<FacebookPage>()
                .Query()
                .Include(page => page.User)
                .FirstOrDefaultAsync(page => page.Id == id);

            return page
                ?? throw new NotFoundException(
                    $"لم يتم العثور على صفحة Facebook بالمعرّف {id}.");
        }

        private async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new BadRequestException("البريد الإلكتروني للمستخدم مطلوب.");
            }

            var normalizedEmail = email.Trim().ToUpperInvariant();

            var user = await _unitOfWork
                .Repository<ApplicationUser>()
                .Query(tracked: false)
                .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail);

            if (user is null)
            {
                throw new NotFoundException($"لا يوجد مستخدم مسجل بالبريد الإلكتروني: {email.Trim()}");
            }

            return user;
        }
    }
}
