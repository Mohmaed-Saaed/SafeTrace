using SafeTrace.Application.DTOs.SharedCases;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>
    /// Class-level attribute that ensures MaxAge &gt;= MinAge when both are provided.
    /// Applied on UrgentCaseFilterDto.
    /// </summary>
    public class AgeRangeValidAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is not CasesFilterDto filter)
                return ValidationResult.Success;

            if (filter.MinAge.HasValue && filter.MaxAge.HasValue && filter.MaxAge.Value < filter.MinAge.Value)
            {
                return new ValidationResult(ErrorMessage ?? "MaxAge must be greater than or equal to MinAge.", [nameof(CasesFilterDto.MaxAge)]);
            }

            return ValidationResult.Success;
        }
    }
}