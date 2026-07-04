using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeTrace.Application.DTOs;
using SafeTrace.Application.DTOs.AiMatching.Response;
using SafeTrace.Application.Exceptions;
using SafeTrace.Application.Interfaces.IServices;
using SafeTrace.Domain.Entities;
using SafeTrace.Domain.Enums;
using SafeTrace.Domain.Interfaces.IUnitOfWork;
using System.Linq.Expressions;

namespace SafeTrace.Application.Helpers
{
    public class FaceMatchingHelper
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFaceRecognitionService _faceRecognition;
        private readonly ILogger<FaceMatchingHelper> _logger;

        public FaceMatchingHelper(
            IUnitOfWork unitOfWork,
            IFaceRecognitionService faceRecognition,
            ILogger<FaceMatchingHelper> logger)
        {
            _unitOfWork = unitOfWork;
            _faceRecognition = faceRecognition;
            _logger = logger;
        }

        public async Task CheckForUnknownCaseMatchAsync(IEnumerable<IFormFile> photos)
        {
            var unknownCaseMatches = new Dictionary<long, UnknownCaseMatchDto>();

            foreach (var photo in photos)
            {
                List<FaceMatchResult> faceMatches;
                try
                {
                    faceMatches = await _faceRecognition.SearchByImageAsync(photo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "تعذّر البحث عن وجوه مشابهة في الصورة المرفوعة أثناء إنشاء الحالة.");
                    continue;
                }

                if (!faceMatches.Any()) continue;

                var matchedFaceIds = faceMatches.Select(f => f.FaceId).ToList();

                var matchedPhotos = await _unitOfWork.Repository<CasePhoto>()
                    .Query(tracked: false, includes: new Expression<Func<CasePhoto, object>>[]
                    {
                        p => p.Case,
                        p => p.Case.Photos
                    })
                    .Where(p =>
                        p.FaceId != null &&
                        matchedFaceIds.Contains(p.FaceId) &&
                        p.Case.CaseType == CaseType.Unknown &&
                        p.Case.Status == CaseStatus.Active)
                    .ToListAsync();

                foreach (var matchedPhoto in matchedPhotos)
                {
                    if (!unknownCaseMatches.ContainsKey(matchedPhoto.CaseId))
                    {
                        var similarity = faceMatches
                            .First(f => f.FaceId == matchedPhoto.FaceId).Similarity ?? 0;

                        var primaryPhoto = matchedPhoto.Case.Photos
                            .FirstOrDefault(p => p.IsPrimary)
                            ?? matchedPhoto.Case.Photos.FirstOrDefault();

                        unknownCaseMatches[matchedPhoto.CaseId] = new UnknownCaseMatchDto
                        {
                            CaseId = matchedPhoto.CaseId,
                            CaseCode = matchedPhoto.Case.CaseCode,
                            MainPhotoPath = primaryPhoto?.ImagePath ?? matchedPhoto.ImagePath,
                            Similarity = (float)Math.Round((double)similarity, 2),
                            Government = matchedPhoto.Case.Government,
                            City = matchedPhoto.Case.City,
                           
                        };
                    }
                }
            }

            if (unknownCaseMatches.Any())
            {
                var sortedMatches = unknownCaseMatches.Values
                    .OrderByDescending(m => m.Similarity)
                    .ToList();

                throw new UnknownCaseMatchException(sortedMatches);
            }
        }

        public async Task<LongTermMissingCase?> FindDuplicateByImageAsync(
            List<LongTermMissingCase> candidates,
            IEnumerable<IFormFile>? newPhotos)
        {
            if (newPhotos is null || !newPhotos.Any())
                return null;

            const float duplicateSimilarityThreshold = 90F;
            var candidateFaceIds = candidates
                .SelectMany(c => c.Photos)
                .Where(p => !string.IsNullOrEmpty(p.FaceId))
                .Select(p => p.FaceId!)
                .ToHashSet();

            if (!candidateFaceIds.Any())
                return null;

            foreach (var photo in newPhotos)
            {
                List<FaceMatchResult> faceMatches;
                try
                {
                    faceMatches = await _faceRecognition.SearchByImageAsync(photo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "تعذّر البحث عن تطابق الصورة أثناء فحص التكرار.");
                    continue;
                }

                var strongMatch = faceMatches
                    .Where(f => candidateFaceIds.Contains(f.FaceId) && (f.Similarity ?? 0) >= duplicateSimilarityThreshold)
                    .OrderByDescending(f => f.Similarity)
                    .FirstOrDefault();

                if (strongMatch is not null)
                {
                    var matchedCase = candidates.First(c =>
                        c.Photos.Any(p => p.FaceId == strongMatch.FaceId));

                    return matchedCase;
                }
            }

            return null;
        }
    }
}