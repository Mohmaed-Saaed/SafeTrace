namespace SafeTrace.Application.DTOs.Cases.Request
{
    public class CaseMatchSubjectInfoDto
    {
        [Required(ErrorMessage = "Gender is required.")]
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value.")]
        public Gender Gender { get; init; }

        [Required(ErrorMessage = "Age is required.")]
        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
        public int Age { get; init; }
    }
}
