using Microsoft.AspNetCore.Http;
using SafeTrace.Application.DTOs.AiMatching.Response;


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

        public async Task<ApiResponse<List<MatchedCaseDto>>> GetMatchingCasesAsync(IFormFile image)
        {
            var faceMatches = await _faceRecognitionService.SearchByImageAsync(image);

            if (faceMatches == null || !faceMatches.Any())
            {
                return ApiResponse<List<MatchedCaseDto>>.Ok(new List<MatchedCaseDto>(), "لم يتم العثور على حالات مطابقة.");
            }

            var matchedFaceIds = faceMatches.Select(f => f.FaceId).ToList();

            var photosQuery = _unitOfWork.Repository<CasePhoto>()
                .Query(tracked: true, includes:
                [
                    p => p.Case,
                    p => p.Case.Photos
                ]);

            var query = photosQuery.Where(p => p.FaceId != null &&
                                               matchedFaceIds.Contains(p.FaceId) &&
                                               p.Case.Status == CaseStatus.Active);

            var matchedPhotos = await EntityFrameworkQueryableExtensions.ToListAsync(query);

            var photosWithSimilarity = matchedPhotos.Select(p => new
            {
                Photo = p,
                Similarity = faceMatches.First(f => f.FaceId == p.FaceId).Similarity
            });

            var topMatchedPhotos = photosWithSimilarity
                .GroupBy(x => x.Photo.CaseId)
                .Select(group => group.OrderByDescending(x => x.Similarity).First())
                .OrderByDescending(x => x.Photo.Case.CreatedAt)
                .ToList();

            var resultList = new List<MatchedCaseDto>();

            foreach (var item in topMatchedPhotos)
            {
                var caseEntity = item.Photo.Case;

                var dto = _mapper.Map<MatchedCaseDto>(caseEntity);

                dto.Similarity = (float)Math.Round((double)(item.Similarity ?? 0), 2);
                dto.MainPhotoPath = item.Photo.ImagePath;

                resultList.Add(dto);
            }

            return ApiResponse<List<MatchedCaseDto>>.Ok(resultList, "تم العثور على الحالات المطابقة بنجاح.");
        }
    }
}