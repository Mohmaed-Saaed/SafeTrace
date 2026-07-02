using SafeTrace.Application.DTOs.Cases.Request;

namespace SafeTrace.Application.Common.Validators.Attributes
{
    /// <summary>
    /// Class-level attribute that ensures ToDate &gt;= FromDate when both are provided.
    /// Applied on UrgentCaseFilterDto.
    /// </summary>
    public class DateRangeValidAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is not CasesFilterBaseDto filter)
                return ValidationResult.Success;

            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.ToDate.Value < filter.FromDate.Value)
            {
                return new ValidationResult(ErrorMessage ?? "ToDate must be greater than or equal to FromDate.", [nameof(CasesFilterBaseDto.ToDate)]);
            }

            return ValidationResult.Success;
        }
    }
}