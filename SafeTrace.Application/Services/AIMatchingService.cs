using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.Exceptions;
using Microsoft.AspNetCore.Identity;


namespace SafeTrace.Application.Services
{
    public class AIMatchingService : IAIMatchingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IMapper _mapper;
        private readonly ILogger<AIMatchingService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AIMatchingService(
            IUnitOfWork unitOfWork,
            IFaceRecognitionService faceRecognitionService,
            IMapper mapper,
            ILogger<AIMatchingService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _faceRecognitionService = faceRecognitionService;
            _mapper = mapper;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<ApiResponse<List<MatchedCaseDto>>> GetMatchingCasesAsync(IFormFile image, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UnauthorizedException("تعذر التحقق من هوية المستخدم.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var isModerator = await _userManager.IsInRoleAsync(user, "Moderator");

            if (!isAdmin && !isModerator)
            {
                var today = DateTime.UtcNow.Date;

                var dailyUsageCount = await EntityFrameworkQueryableExtensions.CountAsync(
                    _unitOfWork.Repository<AiSearchUsage>()
                        .Query(tracked: false)
                        .Where(x => x.UserId == userId && x.CreatedAt.Date == today)
                );

                if (dailyUsageCount >= 2)
                {
                    throw new BadRequestException("عذراً، لقد تجاوزت الحد الأقصى (مرتين) لاستخدام البحث الذكي اليوم. يرجى المحاولة لاحقاً.");
                }
            }

            var faceMatches = await _faceRecognitionService.SearchByImageAsync(image);

            if (!isAdmin)
            {
                await _unitOfWork.Repository<AiSearchUsage>().CreateAsync(new AiSearchUsage 
                { 
                    UserId = userId, 
                    CreatedAt = DateTime.UtcNow 
                });
                await _unitOfWork.SaveAsync();
            }

            if (faceMatches == null || !faceMatches.Any())
            {
                return ApiResponse<List<MatchedCaseDto>>.Ok(new List<MatchedCaseDto>(), "لم يتم العثور على حالات مطابقة.");
            }

            var matchedFaceIds = faceMatches.Select(f => f.FaceId).ToList();

            var similarityDict = faceMatches
                .GroupBy(f => f.FaceId)
                .ToDictionary(g => g.Key, g => g.Max(f => f.Similarity));

            var matchedPhotos = await EntityFrameworkQueryableExtensions.ToListAsync(
                _unitOfWork.Repository<CaseFile>()
                    .Query(tracked: true, includes:
                    [
                        p => p.Case,
                        p => p.Case.CaseFiles,
                        p => p.Case.DuplicateGroups
                    ])
                    .Where(p => p.FaceId != null &&
                                matchedFaceIds.Contains(p.FaceId) &&
                                p.Case.Status == CaseStatus.Active)
            );

            if (!matchedPhotos.Any())
            {
                return ApiResponse<List<MatchedCaseDto>>.Ok(new List<MatchedCaseDto>(), "لم يتم العثور على حالات مطابقة.");
            }

            var photosWithSimilarity = matchedPhotos.Select(p => new
            {
                Photo = p,
                Case = p.Case,
                Similarity = p.FaceId != null && similarityDict.ContainsKey(p.FaceId) ? similarityDict[p.FaceId] : 0
            });

            var topCasesWithSimilarity = photosWithSimilarity
                .GroupBy(x => x.Case.Id)
                .Select(group => new
                {
                    Case = group.First().Case,
                    Similarity = group.Max(x => x.Similarity),
                    BestPhoto = group.OrderByDescending(x => x.Similarity).First().Photo
                })
                .ToList();

            var groupedCases = topCasesWithSimilarity
                .GroupBy(x => 
                {
                    if (x.Case.CaseType == CaseType.Unknown && x.Case.DuplicateGroups.Any())
                    {
                        return x.Case.DuplicateGroups.First().DuplicateGroupId;
                    }
                    return -x.Case.Id;
                })
                .Select(group => group.OrderByDescending(x => x.Similarity).First())
                .OrderByDescending(x => x.Similarity)
                .ToList();

            var resultList = new List<MatchedCaseDto>();

            foreach (var item in groupedCases)
            {
                var dto = _mapper.Map<MatchedCaseDto>(item.Case);
                dto.Similarity = (float)Math.Round((double)(item.Similarity ?? 0), 2);
                dto.MainPhoto = item.BestPhoto.ImagePath;
                resultList.Add(dto);
            }

            return ApiResponse<List<MatchedCaseDto>>.Ok(resultList, "تم العثور على الحالات المطابقة بنجاح.");
        }
    }
}