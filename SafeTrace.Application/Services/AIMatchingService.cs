using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.Exceptions;


namespace SafeTrace.Application.Services
{
    public class AIMatchingService : IAIMatchingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IMapper _mapper;
        private readonly ILogger<AIMatchingService> _logger;

        public AIMatchingService(
            IUnitOfWork unitOfWork,
            IFaceRecognitionService faceRecognitionService,
            IMapper mapper,
            ILogger<AIMatchingService> logger)
        {
            _unitOfWork = unitOfWork;
            _faceRecognitionService = faceRecognitionService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<MatchedCaseDto>>> GetMatchingCasesAsync(IFormFile image, string userId)
        {
            // var today = DateTime.UtcNow.Date;
            
            // var dailyUsageCount = await EntityFrameworkQueryableExtensions.CountAsync(
            //     _unitOfWork.Repository<AiSearchUsage>()
            //         .Query(tracked: false)
            //         .Where(x => x.UserId == userId && x.CreatedAt.Date == today)
            // );

            // if (dailyUsageCount >= 2)
            // {
            //     throw new BadRequestException("عذراً، لقد تجاوزت الحد الأقصى (مرتين) لاستخدام البحث الذكي اليوم. يرجى المحاولة لاحقاً.");
            // }

            var faceMatches = await _faceRecognitionService.SearchByImageAsync(image);

            await _unitOfWork.Repository<AiSearchUsage>().CreateAsync(new AiSearchUsage 
            { 
                UserId = userId, 
                CreatedAt = DateTime.UtcNow 
            });
            await _unitOfWork.SaveAsync();

            if (faceMatches == null || !faceMatches.Any())
            {
                return ApiResponse<List<MatchedCaseDto>>.Ok(new List<MatchedCaseDto>(), "لم يتم العثور على حالات مطابقة.");
            }

            var matchedFaceIds = faceMatches.Select(f => f.FaceId).ToList();

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
                Similarity = faceMatches.First(f => f.FaceId == p.FaceId).Similarity
            });

            var topCasesWithSimilarity = photosWithSimilarity
                .GroupBy(x => x.Case.Id)
                .Select(group => new
                {
                    Case = group.First().Case,
                    Similarity = group.Max(x => x.Similarity)
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
                .Select(group => group.OrderByDescending(x => x.Case.CreatedAt).First())
                .OrderByDescending(x => x.Similarity)
                .ToList();

            var resultList = new List<MatchedCaseDto>();

            foreach (var item in groupedCases)
            {
                var dto = _mapper.Map<MatchedCaseDto>(item.Case);
                dto.Similarity = (float)Math.Round((double)(item.Similarity ?? 0), 2);
                resultList.Add(dto);
            }

            return ApiResponse<List<MatchedCaseDto>>.Ok(resultList, "تم العثور على الحالات المطابقة بنجاح.");
        }
    }
}