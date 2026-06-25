using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.DTOs.Responses;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;

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
                .Query(tracked: true, includes: new System.Linq.Expressions.Expression<Func<CasePhoto, object>>[]
                {
                    p => p.Case,
                    p => p.Case.Photos
                });

            var query = photosQuery.Where(p => p.FaceId != null &&
                                               matchedFaceIds.Contains(p.FaceId) &&
                                               p.Case.Status == CaseStatus.Active);

            var matchedPhotos = await EntityFrameworkQueryableExtensions.ToListAsync(query);

            var resultList = new List<MatchedCaseDto>();

            var uniqueCasesWithPhoto = matchedPhotos
                .GroupBy(p => p.CaseId)
                .Select(group => group.First())
                .OrderByDescending(p => p.Case.CreatedAt)
                .ToList();

            foreach (var photo in uniqueCasesWithPhoto)
            {
                var caseEntity = photo.Case;

                var similarityVal = faceMatches.First(f => f.FaceId == photo.FaceId).Similarity;

                var dto = _mapper.Map<MatchedCaseDto>(caseEntity);

                dto.Similarity = (float)Math.Round((double)(similarityVal ?? 0), 2);

                resultList.Add(dto);
            }

            return ApiResponse<List<MatchedCaseDto>>.Ok(resultList, "تم العثور على الحالات المطابقة بنجاح.");
        }
    }
}