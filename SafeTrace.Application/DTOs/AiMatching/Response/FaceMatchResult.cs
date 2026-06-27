namespace SafeTrace.Application.DTOs.AiMatching.Response
{
    public class FaceMatchResult
    {
        public string FaceId { get; set; } = null!;
        public float? Similarity { get; set; }
    }
}