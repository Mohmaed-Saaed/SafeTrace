using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class DuplicateCheckResult
    {
        /// <summary>
        /// True when cross-type duplicates exist and the client should ask
        /// the user whether to continue with forceCreate.
        /// </summary>
        public bool RequiresConfirmation { get; init; }

        /// <summary>
        /// Cross-type matched cases.
        /// </summary>
        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; init; } = [];
        public bool IsSameTypeDuplicate { get; set; }

        public static DuplicateCheckResult None => new()
        {
            RequiresConfirmation = false,
            MatchedCases = []
        };
    }
}