namespace SafeTrace.Application.DTOs.AiMatching.Response
{
    public sealed class FaceComparisonResult
    {
        public bool Success { get; init; }

        public bool IsSamePerson { get; init; }

        public float Similarity { get; init; }

        public string? Error { get; init; }
    }
}
