using System.Globalization;
using System.Text;
using SafeTrace.Application.DTOs.FacebookImportedPosts.Response;

namespace SafeTrace.Application.Services
{
    public sealed class CaseDuplicateDetectionService : ICaseDuplicateDetectionService
    {
        private const double StrongNameThreshold = 0.75d;
        private const double SimilarNamePartThreshold = 0.85d;

        private readonly IUnitOfWork _unitOfWork;

        public CaseDuplicateDetectionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CaseDuplicateDetectionResultDto> FindDuplicateAsync(
            FacebookImportedPost post)
        {
            ArgumentNullException.ThrowIfNull(post);

            var importedNameParts = GetNameParts(
                post.FName,
                post.SName,
                post.TName,
                post.LName);

            if (importedNameParts.Count < 2 ||
                !post.Gender.HasValue ||
                !post.Age.HasValue ||
                !post.EventDate.HasValue)
            {
                return CaseDuplicateDetectionResultDto.None;
            }

            var gender = post.Gender.Value;
            var age = post.Age.Value;
            var hasPhone = !string.IsNullOrWhiteSpace(post.CommunicationPhone);

            var candidates = await _unitOfWork.Repository<Case>()
                .Query(tracked: false)
                .Where(candidate =>
                    candidate.Status != CaseStatus.Deleted &&
                    candidate.Status != CaseStatus.Rejected &&
                    ((candidate.Gender == gender && candidate.Age == age) ||
                     (hasPhone && candidate.CommunicationPhone != null)))
                .ToListAsync();

            CaseDuplicateDetectionResultDto? strongestMatch = null;
            var strongestScore = 0;

            foreach (var candidate in candidates)
            {
                var nameSimilarity = CalculateNameSimilarity(
                    importedNameParts,
                    GetNameParts(
                        candidate.FName,
                        candidate.SName,
                        candidate.TName,
                        candidate.LName));

                if (nameSimilarity < StrongNameThreshold)
                    continue;

                var phoneMatch = IsSamePhone(
                    post.CommunicationPhone,
                    candidate.CommunicationPhone);

                var demographicsMatch =
                    candidate.Gender == gender &&
                    candidate.Age == age &&
                    IsSameText(post.Government, candidate.Government) &&
                    IsSameText(post.City, candidate.City) &&
                    DateOnly.FromDateTime(candidate.EventDate) == post.EventDate.Value;

                var score = phoneMatch ? 100 : demographicsMatch ? 90 : 0;

                if (score <= strongestScore)
                    continue;

                strongestScore = score;
                strongestMatch = new CaseDuplicateDetectionResultDto(
                    true,
                    candidate.Id,
                    candidate.CaseCode,
                    candidate.CaseType,
                    phoneMatch
                        ? "Strong name match and matching communication phone."
                        : "Strong name match with matching gender, age, governorate, city, and event date.");
            }

            return strongestMatch ?? CaseDuplicateDetectionResultDto.None;
        }

        private static double CalculateNameSimilarity(
            IReadOnlyList<string> source,
            IReadOnlyList<string> target)
        {
            if (source.Count < 2 || target.Count < 2)
                return 0;

            var remainingTargetIndexes = new HashSet<int>(
                Enumerable.Range(0, target.Count));
            var matchCount = 0;

            foreach (var sourcePart in source)
            {
                var bestIndex = -1;
                var bestSimilarity = 0d;

                foreach (var targetIndex in remainingTargetIndexes)
                {
                    var similarity = CalculateStringSimilarity(
                        sourcePart,
                        target[targetIndex]);

                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestIndex = targetIndex;
                    }
                }

                if (bestIndex >= 0 && bestSimilarity >= SimilarNamePartThreshold)
                {
                    matchCount++;
                    remainingTargetIndexes.Remove(bestIndex);
                }
            }

            if (matchCount < 2)
                return 0;

            return (double)matchCount / Math.Max(source.Count, target.Count);
        }

        private static double CalculateStringSimilarity(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.Ordinal))
                return 1d;

            var maxLength = Math.Max(left.Length, right.Length);
            if (maxLength == 0)
                return 1d;

            return 1d - ((double)LevenshteinDistance(left, right) / maxLength);
        }

        private static int LevenshteinDistance(string left, string right)
        {
            var previous = Enumerable.Range(0, right.Length + 1).ToArray();
            var current = new int[right.Length + 1];

            for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
            {
                current[0] = leftIndex;

                for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
                {
                    var substitutionCost =
                        left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;

                    current[rightIndex] = Math.Min(
                        Math.Min(
                            current[rightIndex - 1] + 1,
                            previous[rightIndex] + 1),
                        previous[rightIndex - 1] + substitutionCost);
                }

                (previous, current) = (current, previous);
            }

            return previous[right.Length];
        }

        private static IReadOnlyList<string> GetNameParts(params string?[] values)
        {
            return values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .SelectMany(value => value!.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries))
                .Select(NormalizeText)
                .Where(value => value.Length > 0)
                .ToList();
        }

        private static bool IsSameText(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            return string.Equals(
                NormalizeText(left),
                NormalizeText(right),
                StringComparison.Ordinal);
        }

        private static bool IsSamePhone(string? left, string? right)
        {
            var normalizedLeft = NormalizePhone(left);
            var normalizedRight = NormalizePhone(right);

            return normalizedLeft.Length > 0 &&
                   string.Equals(
                       normalizedLeft,
                       normalizedRight,
                       StringComparison.Ordinal);
        }

        private static string NormalizePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var digits = new StringBuilder();

            foreach (var character in value)
            {
                if (!char.IsDigit(character))
                    continue;

                var numericValue = char.GetNumericValue(character);
                if (numericValue is >= 0 and <= 9)
                    digits.Append((char)('0' + (int)numericValue));
            }

            var normalized = digits.ToString();

            if (normalized.StartsWith("0020", StringComparison.Ordinal))
                normalized = normalized[4..];
            else if (normalized.StartsWith("20", StringComparison.Ordinal) &&
                     normalized.Length == 12)
                normalized = normalized[2..];

            if (normalized.Length == 10 && normalized[0] == '1')
                normalized = $"0{normalized}";

            return normalized;
        }

        private static string NormalizeText(string value)
        {
            var normalized = value
                .Trim()
                .ToLowerInvariant()
                .Replace('أ', 'ا')
                .Replace('إ', 'ا')
                .Replace('آ', 'ا')
                .Replace('ى', 'ي')
                .Replace('ة', 'ه')
                .Replace('ؤ', 'و')
                .Replace('ئ', 'ي')
                .Normalize(NormalizationForm.FormD);

            var result = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) ==
                    UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                    result.Append(character);
            }

            return result.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
