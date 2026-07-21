namespace SafeTrace.Application.DTOs.UrgentCase.Response
{
    public class UrgentCreationStatusResponse
    {
        public bool IsAllowed { get; set; }
        public int? RemainingMinutes { get; set; }
    }
}
