using SafeTrace.Application.DTOs.FacebookPages.Request;
using SafeTrace.Application.DTOs.FacebookPages.Response;
using SafeTrace.Application.Exceptions;

namespace SafeTrace.Application.Services
{
    public class FacebookPageService : IFacebookPageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFacebookGraphService _facebookGraphService;
        private readonly IMapper _mapper;

        public FacebookPageService(
            IUnitOfWork unitOfWork,
            IFacebookGraphService facebookGraphService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _facebookGraphService = facebookGraphService;
            _mapper = mapper;
        }

        public async Task<FacebookPageResponseDto> CreateAsync(CreateFacebookPageDto dto)
        {
            var facebookPageId = dto.FacebookPageId.Trim();

            var pageExists = await _unitOfWork.Repository<FacebookPage>().AnyAsync(page => page.FacebookPageId == facebookPageId);

            if (pageExists)
            {
                throw new ConflictException("صفحة Facebook بهذا المعرّف مسجلة بالفعل.");
            }

            var user = await GetUserByEmailAsync(dto.UserEmail);

            var page = _mapper.Map<FacebookPage>(dto);

            page.UserId = user.Id;
            page.User = user;
            page.IntegrationStatus =FacebookIntegrationStatus.Disconnected;
            page.IsActive = false;
            page.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<FacebookPage>().CreateAsync(page);

            await _unitOfWork.SaveAsync();

            return _mapper.Map<FacebookPageResponseDto>(page);
        }

        public async Task<FacebookPageResponseDto> UpdateAsync(long id, UpdateFacebookPageDto dto)
        {
            var page = await GetEntityWithUserAsync(id);

            var user = await GetUserByEmailAsync(dto.UserEmail);

            _mapper.Map(dto, page);

            page.UserId = user.Id;
            page.User = user;
            page.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveAsync();

            return _mapper.Map<FacebookPageResponseDto>(page);
        }

        public async Task<List<FacebookPageResponseDto>> GetAllAsync()
        {
            var pages = await _unitOfWork
                .Repository<FacebookPage>()
                .Query(tracked: false)
                .Include(page => page.User)
                .OrderByDescending(page => page.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<FacebookPageResponseDto>>(pages);
        }

        public async Task<FacebookPageResponseDto> GetByIdAsync(long id)
        {
            var page = await _unitOfWork
                .Repository<FacebookPage>()
                .Query(tracked: false)
                .Include(page => page.User)
                .FirstOrDefaultAsync(page =>
                    page.Id == id);

            if (page is null)
            {
                throw new NotFoundException($"لم يتم العثور على صفحة Facebook بالمعرّف {id}.");
            }

            return _mapper.Map<FacebookPageResponseDto>(page);
        }

        public async Task<FacebookPageResponseDto> ConnectAsync(long id)
        {
            var page = await GetEntityWithUserAsync(id);

            if (page.IntegrationStatus == FacebookIntegrationStatus.Connected)
            {
                throw new BadRequestException("صفحة Facebook متصلة بالفعل.");
            }

            var connection = await _facebookGraphService.ConnectPageAsync(page.FacebookPageId);

            ApplySuccessfulConnection(page, connection);

            await _unitOfWork.SaveAsync();

            return _mapper.Map<FacebookPageResponseDto>(page);
        }

        public async Task<FacebookPageResponseDto> ReconnectAsync(long id)
        {
            var page = await GetEntityWithUserAsync(id);

            if (page.IntegrationStatus != FacebookIntegrationStatus.NeedsReconnect &&
                page.IntegrationStatus != FacebookIntegrationStatus.Disconnected)
            {
                throw new BadRequestException("يمكن إعادة ربط صفحات Facebook غير المتصلة أو التي تحتاج إلى إعادة اتصال فقط.");
            }

            var connection = await _facebookGraphService.ReconnectPageAsync(page.FacebookPageId);

            ApplySuccessfulConnection(page, connection);

            await _unitOfWork.SaveAsync();

            return _mapper.Map<FacebookPageResponseDto>(page);
        }

        public async Task<FacebookPageResponseDto> DisconnectAsync(long id)
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

            return _mapper.Map<FacebookPageResponseDto>(page);
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
                    $"Facebook page with id {id} was not found.");
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

        private static void ApplySuccessfulConnection(FacebookPage page, FacebookPageConnectionResultDto connection)
        {
            if (!string.Equals(
                    page.FacebookPageId,
                    connection.FacebookPageId,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(connection.PageAccessToken))
            {
                throw new BadRequestException("تعذر التحقق من صلاحية الوصول إلى صفحة Facebook المحددة.");
            }

            page.PageAccessToken = connection.PageAccessToken;
            page.TokenExpiresAt = connection.TokenExpiresAt;
            page.IntegrationStatus = FacebookIntegrationStatus.Connected;
            page.IsActive = true;
            page.UpdatedAt = DateTime.UtcNow;
        }
    }
}
