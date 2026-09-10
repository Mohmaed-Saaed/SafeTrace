namespace SafeTrace.Application.DTOs.Cases.Response
{
    public class UserDto
    {
        public string? Id { get; set; }
        public string FName { get; set; } = null!;
        public string LName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? ProfileImage { get; set; }
    }
}
