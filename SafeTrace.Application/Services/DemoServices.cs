using FluentValidation;
using SafeTrace.Application.DTOs.UrgentMissingCase.Request;

namespace SafeTrace.Application.Validators
{
    // ────────────────────────────────────────────────────────────────────────────
    // Create Validator
    // ────────────────────────────────────────────────────────────────────────────
    public class UrgentCaseCreateValidator : AbstractValidator<UrgentCaseCreateDto>
    {
        private const int MaxPhotoCount    = 5;
        private const long MaxPhotoBytes   = 5 * 1024 * 1024; // 5 MB per file
        private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

        public UrgentCaseCreateValidator()
        {
            // ── Location ────────────────────────────────────────────────
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.");

            // ── Demographics ────────────────────────────────────────────
            RuleFor(x => x.Age)
                .InclusiveBetween(0, 120)
                .WithMessage("Age must be between 0 and 120.");

            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithMessage("Invalid gender value.");

            RuleFor(x => x.Relation)
                .IsInEnum()
                .WithMessage("Invalid relation type.");

            // ── Address ─────────────────────────────────────────────────
            RuleFor(x => x.Government)
                .NotEmpty().WithMessage("Government is required.")
                .MaximumLength(100);

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100);

            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Street is required.")
                .MaximumLength(200);

            // ── Names ───────────────────────────────────────────────────
            RuleFor(x => x.FName).MaximumLength(60).When(x => x.FName != null);
            RuleFor(x => x.SName).MaximumLength(60).When(x => x.SName != null);
            RuleFor(x => x.TName).MaximumLength(60).When(x => x.TName != null);
            RuleFor(x => x.LName).MaximumLength(60).When(x => x.LName != null);

            // ── Phone ───────────────────────────────────────────────────
            RuleFor(x => x.CommunicationPhone)
                .MaximumLength(20)
                .Matches(@"^\+?[0-9\s\-()]{7,20}$")
                .WithMessage("Invalid phone number format.")
                .When(x => !string.IsNullOrEmpty(x.CommunicationPhone));

            // ── Event Date ──────────────────────────────────────────────
            RuleFor(x => x.EventDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Event date cannot be in the future.")
                .GreaterThan(DateTime.UtcNow.AddYears(-1))
                .WithMessage("Event date cannot be more than 1 year in the past.");

            // ── Description ─────────────────────────────────────────────
            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description != null);

            // ── Photos ──────────────────────────────────────────────────
            RuleFor(x => x.Photos)
                .Must(p => p == null || p.Count <= MaxPhotoCount)
                .WithMessage($"You can upload a maximum of {MaxPhotoCount} photos.");

            RuleForEach(x => x.Photos)
                .ChildRules(photo =>
                {
                    photo.RuleFor(f => f.Length)
                         .LessThanOrEqualTo(MaxPhotoBytes)
                         .WithMessage($"Each photo must be at most {MaxPhotoBytes / 1024 / 1024} MB.");

                    photo.RuleFor(f => f.ContentType)
                         .Must(ct => AllowedMimeTypes.Contains(ct))
                         .WithMessage("Only JPEG, PNG, and WebP images are allowed.");
                })
                .When(x => x.Photos != null && x.Photos.Any());
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Update Validator
    // ────────────────────────────────────────────────────────────────────────────
    public class UrgentCaseUpdateValidator : AbstractValidator<UrgentCaseUpdateDto>
    {
        private const int MaxPhotoCount  = 5;
        private const long MaxPhotoBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

        public UrgentCaseUpdateValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("A valid case ID is required.");

            RuleFor(x => x.Age)
                .InclusiveBetween(0, 120)
                .WithMessage("Age must be between 0 and 120.")
                .When(x => x.Age.HasValue);

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue);

            RuleFor(x => x.Government).MaximumLength(100).When(x => x.Government != null);
            RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
            RuleFor(x => x.Street).MaximumLength(200).When(x => x.Street != null);
            RuleFor(x => x.FName).MaximumLength(60).When(x => x.FName != null);
            RuleFor(x => x.SName).MaximumLength(60).When(x => x.SName != null);
            RuleFor(x => x.TName).MaximumLength(60).When(x => x.TName != null);
            RuleFor(x => x.LName).MaximumLength(60).When(x => x.LName != null);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);

            RuleFor(x => x.CommunicationPhone)
                .MaximumLength(20)
                .Matches(@"^\+?[0-9\s\-()]{7,20}$")
                .When(x => !string.IsNullOrEmpty(x.CommunicationPhone));

            RuleFor(x => x.EventDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Event date cannot be in the future.")
                .When(x => x.EventDate.HasValue);

            RuleFor(x => x.Photos)
                .Must(p => p == null || p.Count <= MaxPhotoCount)
                .WithMessage($"Maximum {MaxPhotoCount} photos per upload.");

            // Validate DeletedPhotoIds is non-empty if provided
            RuleFor(x => x.DeletedPhotoIds)
                .Must(ids => ids == null || ids.All(id => id > 0))
                .WithMessage("Photo IDs to delete must be positive integers.");

            RuleForEach(x => x.Photos)
                .ChildRules(photo =>
                {
                    photo.RuleFor(f => f.Length)
                         .LessThanOrEqualTo(MaxPhotoBytes);
                    photo.RuleFor(f => f.ContentType)
                         .Must(ct => AllowedMimeTypes.Contains(ct));
                })
                .When(x => x.Photos != null && x.Photos.Any());
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Filter Validator
    // ────────────────────────────────────────────────────────────────────────────
    public class UrgentCaseFilterValidator : AbstractValidator<UrgentCaseFilterDto>
    {
        public UrgentCaseFilterValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");

            RuleFor(x => x.MinAge)
                .InclusiveBetween(0, 120)
                .When(x => x.MinAge.HasValue);

            RuleFor(x => x.MaxAge)
                .InclusiveBetween(0, 120)
                .When(x => x.MaxAge.HasValue);

            // MaxAge must be >= MinAge when both are provided
            RuleFor(x => x.MaxAge)
                .GreaterThanOrEqualTo(x => x.MinAge!.Value)
                .WithMessage("MaxAge must be greater than or equal to MinAge.")
                .When(x => x.MinAge.HasValue && x.MaxAge.HasValue);

            RuleFor(x => x.RadiusInMeters)
                .InclusiveBetween(100, 500_000)
                .WithMessage("Radius must be between 100 m and 500 km.");

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate!.Value)
                .WithMessage("ToDate must be after FromDate.")
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

            RuleFor(x => x.Gender)
                .IsInEnum()
                .When(x => x.Gender.HasValue);

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue);
        }
    }
}