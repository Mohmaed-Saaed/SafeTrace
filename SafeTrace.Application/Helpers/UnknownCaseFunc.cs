using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SafeTrace.Application.Helpers
{
    public class UnknownCaseFunc 

    {
        private readonly IFaceRecognitionService _faceRecognitionService;
        private readonly IUnitOfWork _unitOfWork;

        public UnknownCaseFunc(
            IFaceRecognitionService faceRecognitionService,
            IUnitOfWork unitOfWork)
        {
            _faceRecognitionService = faceRecognitionService;
            _unitOfWork = unitOfWork;
        }
        public async Task LinkCaseToDuplicateGroupAsync(
     UnknownCase newCase,
     IFormFile primaryImage)
        {
            var matches = await _faceRecognitionService.SearchByImageAsync(primaryImage);

            if (matches == null || !matches.Any())
            {
                await CreateDuplicateGroupAsync(newCase);

                return;
            }

            var bestMatch = matches
                .OrderByDescending(x => x.Similarity)
                .First();

            if ((bestMatch.Similarity ?? 0) < 95)
            {
                await CreateDuplicateGroupAsync(newCase);

                return;
            }

            var matchedCase = await GetMatchedCaseAsync(
                bestMatch.FaceId!,
                newCase.Id);

            if (matchedCase == null)
            {
                await CreateDuplicateGroupAsync(newCase);

                return;
            }

            var groupLink = await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .Query(tracked: true)
                .FirstOrDefaultAsync(x => x.CaseId == matchedCase.Id);

            if (groupLink == null)
            {
                await CreateDuplicateGroupWithCasesAsync(
                    matchedCase,
                    newCase,
                    (decimal)(bestMatch.Similarity ?? 100));

                return;
            }

            await AddCaseToGroupAsync(
                groupLink.DuplicateGroupId,
                newCase.Id,
               (decimal) (bestMatch.Similarity ?? 100));
        }
        private async Task<UnknownCase?> GetMatchedCaseAsync(
    string faceId,
    long currentCaseId)
        {
            return await _unitOfWork
                .Repository<CaseFile>()
                .Query(
                    tracked: true,
                    includes:
                    [
                        x => x.Case
                    ])
                .Where(x =>
                    x.FaceId == faceId &&
                    x.CaseId != currentCaseId &&
                    x.Case is UnknownCase &&
                    x.Case.Status == CaseStatus.Active)
                .Select(x => (UnknownCase)x.Case)
                .FirstOrDefaultAsync();
        }
        private async Task CreateDuplicateGroupAsync(
    UnknownCase newCase)
        {
            var group = new DuplicateGroup
            {
                GroupStatus = DuplicateGroupStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork
                .Repository<DuplicateGroup>()
                .CreateAsync(group);

            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroup = group,
                    CaseId = newCase.Id,
                    SimilarityScore = 100,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });
        }


        private async Task CreateDuplicateGroupWithCasesAsync(
     UnknownCase oldCase,
     UnknownCase newCase,
     decimal similarity)
        {
            var group = new DuplicateGroup
            {
                GroupStatus = DuplicateGroupStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork
                .Repository<DuplicateGroup>()
                .CreateAsync(group);

            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroup = group,
                    CaseId = oldCase.Id,
                    SimilarityScore = 100,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });

            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroup = group,
                    CaseId = newCase.Id,
                    SimilarityScore = similarity,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });
        }
        private async Task AddCaseToGroupAsync(
    long groupId,
    long caseId,
    decimal similarity)
        {
            var exists = await _unitOfWork
        .Repository<DuplicateGroupCase>()
        .Query()
        .AnyAsync(x =>
        x.DuplicateGroupId == groupId &&
        x.CaseId == caseId);

            if (exists)
                return;

            await _unitOfWork
                .Repository<DuplicateGroupCase>()
                .CreateAsync(new DuplicateGroupCase
                {
                    DuplicateGroupId = groupId,
                    CaseId = caseId,
                    SimilarityScore = similarity,
                    MatchedBy = DuplicateMatchType.AI,
                    CreatedAt = DateTime.UtcNow
                });
        }
    }
}
