namespace SafeTrace.Application.DTOs.FacebookImportedPosts.Response
{
    public sealed record FacebookPostRequirementResultDto(
        bool IsComplete,
        IReadOnlyList<string> MissingRequirements);
}
