namespace SafeTrace.Application.DTOs.Cases.Request
{
    public class FoundPersonInfoRequestDto
    {
        public string Description { get; set; } = null!;
        public string Government { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Street { get; set; } = null!;
        public DateOnly FoundedAt { get; set; }
    }
}