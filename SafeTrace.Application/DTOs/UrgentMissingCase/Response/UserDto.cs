namespace SafeTrace.Application.DTOs.UrgentMissingCase.Response;

public class UserDto
{
    public string Id { get; set; } = null!;
    public string FName { get; set; } = null!;
    public string LName { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}