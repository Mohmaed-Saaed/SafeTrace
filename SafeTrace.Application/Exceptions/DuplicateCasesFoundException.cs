using SafeTrace.Application.DTOs.AiMatching.Response;

namespace SafeTrace.Application.Exceptions
{
    public class DuplicateCasesFoundException : ConflictException
    {
        public IReadOnlyList<MatchedCaseDto> MatchedCases { get; }

        public DuplicateCasesFoundException(IReadOnlyList<MatchedCaseDto> matchedCases)
            : base("تم العثور على حالات مشابهة.")
        {
            MatchedCases = matchedCases;
        }
    }
}

