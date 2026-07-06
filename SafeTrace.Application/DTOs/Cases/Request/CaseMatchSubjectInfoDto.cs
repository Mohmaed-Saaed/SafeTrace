
namespace SafeTrace.Application.DTOs.Cases.Request
{
    /// <summary>
    /// The subset of a case-creation request needed to verify whether a candidate face
    /// match is actually the same person. Deliberately decoupled from any specific
    /// Create*Dto (Urgent/LongTerm/Unknown don't share a base request type today), so
    /// each case service just maps its own DTO into this before calling the helper.
    /// </summary>
    public class CaseMatchSubjectInfoDto
    {
        public Gender Gender { get; init; }
        public int Age { get; init; }
    }
}

